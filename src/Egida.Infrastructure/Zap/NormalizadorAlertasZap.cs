using System.Text.Json;
using Egida.Domain.Clasificacion;
using Egida.Domain.Escaneos;

namespace Egida.Infrastructure.Zap;

/// <summary>
/// Convierte alertas de la API JSON de ZAP en hallazgos normalizados (research R3):
/// riesgo → severidad de 4 niveles; etiquetas/CWE → OWASP Top 10:2025; se descartan las alertas
/// con confianza "False Positive" (0) y se agrupan las repetidas (plugin + URL + parámetro;
/// o solo plugin cuando la alerta no tiene parámetro).
/// </summary>
public static class NormalizadorAlertasZap
{
    public static IReadOnlyList<DatosHallazgo> Normalizar(JsonElement alertas)
    {
        var resultado = new List<(string Clave, DatosHallazgo Datos)>();
        foreach (var a in alertas.EnumerateArray())
        {
            var confianza = Confianza(a);
            if (confianza == 0) continue;

            var pluginId = int.TryParse(Texto(a, "pluginId"), out var p) ? p : 0;
            var url = Texto(a, "url") ?? string.Empty;
            var parametro = Texto(a, "param");
            var cwe = int.TryParse(Texto(a, "cweid"), out var c) && c > 0 ? c : (int?)null;
            var etiquetas = a.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Object
                ? t.EnumerateObject().Select(x => x.Name).ToList()
                : [];
            var categoria = MapeadorOwasp.Mapear(etiquetas, cwe);
            var recomendacion = Texto(a, "solution");
            if (string.IsNullOrWhiteSpace(recomendacion)) recomendacion = MapeadorOwasp.RecomendacionPara(categoria);

            // Agrupación: con parámetro, por plugin + URL + parámetro; sin parámetro (p. ej. cabeceras
            // ausentes en todas las páginas), por plugin, contando las URL afectadas en Ocurrencias.
            var clave = string.IsNullOrEmpty(parametro) ? $"{pluginId}" : $"{pluginId}|{url}|{parametro}";
            var existente = resultado.FindIndex(r => r.Clave == clave);
            if (existente >= 0)
            {
                var d = resultado[existente].Datos;
                resultado[existente] = (clave, d with { Ocurrencias = d.Ocurrencias + 1 });
                continue;
            }

            resultado.Add((clave, new DatosHallazgo(
                Nombre: Texto(a, "alert") ?? Texto(a, "name") ?? "Hallazgo sin nombre",
                Descripcion: Texto(a, "description") ?? string.Empty,
                Severidad: Severidad(a),
                Categoria: categoria,
                UrlAfectada: url,
                Recomendacion: recomendacion,
                PluginId: pluginId,
                ConfianzaEscaner: Math.Clamp(confianza, 1, 3),
                MetodoHttp: Texto(a, "method"),
                Parametro: parametro,
                Evidencia: Texto(a, "evidence"),
                CweId: cwe)));
        }
        return resultado.Select(r => r.Datos).ToList();
    }

    /// <summary>riskcode 3/2/1/0 (o el texto High/Medium/Low/Informational) → Alta/Media/Baja/Informativa.</summary>
    public static Severidad? Severidad(JsonElement a)
    {
        var codigo = Texto(a, "riskcode");
        if (int.TryParse(codigo, out var n) && n is >= 0 and <= 3) return (Severidad)n;
        return Texto(a, "risk")?.Trim().ToLowerInvariant() switch
        {
            "high" => Domain.Escaneos.Severidad.Alta,
            "medium" => Domain.Escaneos.Severidad.Media,
            "low" => Domain.Escaneos.Severidad.Baja,
            "informational" or "info" => Domain.Escaneos.Severidad.Informativa,
            _ => null,
        };
    }

    /// <summary>0 = falso positivo marcado por ZAP; 1 Baja; 2 Media; 3 Alta/Confirmada.</summary>
    public static int Confianza(JsonElement a)
    {
        if (int.TryParse(Texto(a, "confidencecode"), out var n)) return Math.Min(n, 3);
        return Texto(a, "confidence")?.Trim().ToLowerInvariant() switch
        {
            "false positive" => 0,
            "low" => 1,
            "medium" => 2,
            "high" or "confirmed" => 3,
            _ => 2,
        };
    }

    private static string? Texto(JsonElement a, string propiedad) =>
        a.TryGetProperty(propiedad, out var v)
            ? v.ValueKind switch
            {
                JsonValueKind.String => string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null,
            }
            : null;
}
