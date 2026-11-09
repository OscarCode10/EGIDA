using System.Text.Json;
using System.Text.RegularExpressions;
using Egida.Application.Abstracciones;
using Egida.Application.Escaneos;
using Egida.Domain.Activos;
using Egida.Domain.Escaneos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Egida.Infrastructure.Zap;

public sealed class OpcionesZap
{
    public const string Seccion = "Zap";
    public string Url { get; set; } = "http://zap:8080";
    public string ClaveApi { get; set; } = string.Empty;
}

/// <summary>
/// Perfil de escaneo "demo" (research R1/R2) sobre la API REST de ZAP:
/// 1) sesión nueva y contexto limitado al origen autorizado; 2) visita de URLs semilla;
/// 3) spider acotado; 4) espera del análisis pasivo; 5) escaneo activo con política reducida
/// y tiempo limitado; 6) lectura de alertas del origen; 7) eliminación del contexto.
/// Las pruebas activas solo se permiten porque el objetivo está en la lista blanca (principio XII).
/// </summary>
public sealed class ClienteZap(HttpClient http, IOptions<OpcionesZap> opcionesZap,
    IOptions<OpcionesEscaneo> opcionesEscaneo, ILogger<ClienteZap> log) : IEscaner
{
    public const string NombrePolitica = "egida-demo";

    /// <summary>
    /// Política reducida: SQLi, XSS reflejado/persistente, path traversal, inclusión remota,
    /// listado de directorios, inyección de comandos y SSI (IDs válidos en zap-stable 2.17).
    /// </summary>
    public static readonly int[] ReglasActivas = [40018, 40012, 40014, 40016, 40017, 6, 7, 0, 90020, 40009];

    public async Task<IReadOnlyList<DatosHallazgo>> EscanearAsync(ObjetivoPermitido objetivo, string urlActivo,
        CancellationToken ct)
    {
        var origen = UrlNormalizada.Crear(objetivo.Url).Origen;
        var o = opcionesEscaneo.Value;
        var contexto = $"egida-{Guid.NewGuid():N}";

        await LlamarAsync("core/action/newSession/", ct, ("overwrite", "true"));
        var contextoId = Cadena(await LlamarAsync("context/action/newContext/", ct, ("contextName", contexto)), "contextId");
        try
        {
            // Alcance: solo el origen autorizado (segunda barrera además de la lista blanca, FR-013).
            await LlamarAsync("context/action/includeInContext/", ct,
                ("contextName", contexto), ("regex", Regex.Escape(origen) + "(/.*)?"));

            foreach (var ruta in new[] { "/" }.Concat(objetivo.UrlsSemilla))
            {
                try
                {
                    await LlamarAsync("core/action/accessUrl/", ct, ("url", origen + ruta), ("followRedirects", "false"));
                }
                catch (HttpRequestException ex) when (ruta != "/")
                {
                    log.LogWarning("URL semilla no accesible {Ruta}: {Error}", ruta, ex.Message);
                }
            }

            var spider = Cadena(await LlamarAsync("spider/action/scan/", ct, ("url", origen), ("contextName", contexto),
                ("recurse", "true"), ("subtreeOnly", "true")), "scan");
            await EsperarAsync("spider/view/status/", spider, o.SegundosSpider, ct);
            await LlamarAsync("spider/action/stop/", ct, ("scanId", spider));

            await EsperarPasivoAsync(o.SegundosPasivo, ct);

            await PrepararPoliticaAsync(ct);
            var activo = Cadena(await LlamarAsync("ascan/action/scan/", ct, ("url", origen), ("recurse", "true"),
                ("inScopeOnly", "false"), ("scanPolicyName", NombrePolitica), ("contextId", contextoId)), "scan");
            await EsperarAsync("ascan/view/status/", activo, o.SegundosEscaneoActivo, ct);
            await LlamarAsync("ascan/action/stop/", ct, ("scanId", activo));
            await EsperarPasivoAsync(15, ct);

            var alertas = await LlamarAsync("core/view/alerts/", ct, ("baseurl", origen), ("start", "0"), ("count", "10000"));
            var hallazgos = NormalizadorAlertasZap.Normalizar(alertas.GetProperty("alerts"))
                .Where(h => h.UrlAfectada.StartsWith(origen, StringComparison.OrdinalIgnoreCase))
                .ToList();
            log.LogInformation("ZAP devolvió {Total} hallazgos para {Origen}", hallazgos.Count, origen);
            return hallazgos;
        }
        finally
        {
            try
            {
                await LlamarAsync("context/action/removeContext/", CancellationToken.None, ("contextName", contexto));
            }
            catch (Exception ex)
            {
                log.LogWarning("No se pudo eliminar el contexto {Contexto}: {Error}", contexto, ex.Message);
            }
        }
    }

    private async Task PrepararPoliticaAsync(CancellationToken ct)
    {
        var nombres = await LlamarAsync("ascan/view/scanPolicyNames/", ct);
        var existe = nombres.GetProperty("scanPolicyNames").EnumerateArray().Any(n => n.GetString() == NombrePolitica);
        if (!existe)
            await LlamarAsync("ascan/action/addScanPolicy/", ct, ("scanPolicyName", NombrePolitica),
                ("alertThreshold", "MEDIUM"), ("attackStrength", "LOW"));
        await LlamarAsync("ascan/action/disableAllScanners/", ct, ("scanPolicyName", NombrePolitica));
        await LlamarAsync("ascan/action/enableScanners/", ct, ("ids", string.Join(',', ReglasActivas)),
            ("scanPolicyName", NombrePolitica));
    }

    private async Task EsperarAsync(string vista, string scanId, int segundosMax, CancellationToken ct)
    {
        var limite = DateTimeOffset.UtcNow.AddSeconds(segundosMax);
        while (DateTimeOffset.UtcNow < limite)
        {
            var estado = Cadena(await LlamarAsync(vista, ct, ("scanId", scanId)), "status");
            if (estado == "100") return;
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }
        log.LogInformation("Se alcanzó el tiempo máximo de {Vista} ({Segundos} s); se detiene", vista, segundosMax);
    }

    private async Task EsperarPasivoAsync(int segundosMax, CancellationToken ct)
    {
        var limite = DateTimeOffset.UtcNow.AddSeconds(segundosMax);
        while (DateTimeOffset.UtcNow < limite)
        {
            if (Cadena(await LlamarAsync("pscan/view/recordsToScan/", ct), "recordsToScan") == "0") return;
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    private async Task<JsonElement> LlamarAsync(string ruta, CancellationToken ct, params (string Clave, string Valor)[] parametros)
    {
        var consulta = string.Join('&', parametros.Append(("apikey", opcionesZap.Value.ClaveApi))
            .Select(p => $"{Uri.EscapeDataString(p.Item1)}={Uri.EscapeDataString(p.Item2)}"));
        using var respuesta = await http.GetAsync($"JSON/{ruta}?{consulta}", ct);
        var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
        if (!respuesta.IsSuccessStatusCode)
            throw new HttpRequestException($"ZAP respondió {(int)respuesta.StatusCode} en {ruta}: {Recortar(cuerpo)}");
        using var doc = JsonDocument.Parse(cuerpo);
        return doc.RootElement.Clone();
    }

    private static string Cadena(JsonElement e, string propiedad) =>
        e.TryGetProperty(propiedad, out var v) ? v.ToString() : throw new HttpRequestException($"Respuesta de ZAP sin '{propiedad}'.");

    private static string Recortar(string s) => s.Length <= 200 ? s : s[..200];
}
