using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Egida.Application.Abstracciones;
using Egida.Domain.Escaneos;
using Microsoft.Extensions.Logging;

namespace Egida.Infrastructure.IA;

public sealed class OpcionesIA
{
    public const string Seccion = "IA";
    public string Url { get; set; } = "http://egida-ia:8000";
}

/// <summary>
/// Cliente del microservicio egida-ia (contracts/ia-analizar.md). Solo REST (principio V).
/// Si la respuesta no cumple el contrato, el lote completo se trata como no analizado (FR-024).
/// </summary>
public sealed class ClienteIA(HttpClient http, ILogger<ClienteIA> log) : IAnalizadorIA
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    public async Task<RespuestaIA?> AnalizarAsync(Guid escaneoId, IReadOnlyList<Hallazgo> hallazgos, CancellationToken ct)
    {
        if (hallazgos.Count == 0) return new RespuestaIA("sin-datos", []);

        var solicitud = new SolicitudAnalizar(escaneoId.ToString(), hallazgos.Select(h => new HallazgoIA(
            h.Id.ToString(), h.Nombre, h.PluginId, h.Severidad.ToString(), h.ConfianzaEscaner, h.CweId,
            h.CategoriaOwasp, h.UrlAfectada, h.MetodoHttp, h.Parametro, h.Evidencia, h.TipoContenido,
            h.Ocurrencias)).ToList());

        try
        {
            using var respuesta = await http.PostAsJsonAsync("analizar", solicitud, Json, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                log.LogWarning("egida-ia respondió {Codigo}", (int)respuesta.StatusCode);
                return null;
            }
            var cuerpo = await respuesta.Content.ReadFromJsonAsync<RespuestaAnalizar>(Json, ct);
            return Validar(cuerpo, hallazgos);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       or Polly.CircuitBreaker.BrokenCircuitException or TimeoutException
                                       or Polly.Timeout.TimeoutRejectedException)
        {
            if (ct.IsCancellationRequested) throw;
            log.LogWarning("egida-ia no disponible: {Error}", ex.Message);
            return null;
        }
    }

    public async Task<bool> DisponibleAsync(CancellationToken ct)
    {
        try
        {
            using var r = await http.GetAsync("salud", ct);
            return r.IsSuccessStatusCode;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>Exactamente un resultado por id, prioridad conocida y probabilidad en [0, 1].</summary>
    public static RespuestaIA? Validar(RespuestaAnalizar? cuerpo, IReadOnlyList<Hallazgo> hallazgos)
    {
        if (cuerpo?.Resultados is null) return null;
        var esperados = hallazgos.Select(h => h.Id).ToHashSet();
        var resultados = new List<ResultadoIA>();
        foreach (var r in cuerpo.Resultados)
        {
            if (!Guid.TryParse(r.Id, out var id) || !esperados.Remove(id)) return null;
            if (!Enum.TryParse<PrioridadIA>(r.Prioridad, false, out var prioridad) || !Enum.IsDefined(prioridad)) return null;
            if (r.ProbabilidadFalsoPositivo is not { } p || p < 0 || p > 1) return null;
            if (string.IsNullOrWhiteSpace(r.Justificacion)) return null;
            resultados.Add(new ResultadoIA(id, prioridad, (decimal)p, r.Justificacion));
        }
        return esperados.Count == 0 ? new RespuestaIA(cuerpo.VersionModelo ?? "desconocida", resultados) : null;
    }

    public sealed record SolicitudAnalizar(string EscaneoId, List<HallazgoIA> Hallazgos);

    public sealed record HallazgoIA(string Id, string Nombre, int PluginId, string Severidad, int ConfianzaEscaner,
        int? CweId, string CategoriaOwasp, string Url, string? MetodoHttp, string? Parametro, string? Evidencia,
        string? TipoContenido, int Ocurrencias);

    public sealed record RespuestaAnalizar(string? EscaneoId, string? VersionModelo, List<ResultadoAnalizar>? Resultados);

    public sealed record ResultadoAnalizar(string? Id, string? Prioridad, double? ProbabilidadFalsoPositivo, string? Justificacion);
}
