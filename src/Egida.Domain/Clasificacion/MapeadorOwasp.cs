using System.Text.RegularExpressions;

namespace Egida.Domain.Clasificacion;

/// <summary>
/// Regla documentada de asignación de categoría OWASP Top 10:2025 (research R3, FR-020).
/// Orden: (1) etiqueta OWASP_2025_Axx de ZAP; (2) etiqueta OWASP_2021_Axx traducida con la
/// tabla 2021→2025; (3) tabla CWE→2025; (4) categoría por defecto A02 (Security Misconfiguration).
/// Ningún hallazgo queda sin categoría.
/// </summary>
public static partial class MapeadorOwasp
{
    [GeneratedRegex(@"^OWASP_(2025|2021)_(A\d{2})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EtiquetaOwasp();

    public static CategoriaOwasp Mapear(IEnumerable<string>? etiquetas, int? cweId)
    {
        string? de2025 = null, de2021 = null;
        foreach (var etiqueta in etiquetas ?? [])
        {
            var m = EtiquetaOwasp().Match(etiqueta.Trim());
            if (!m.Success) continue;
            var codigo = m.Groups[2].Value.ToUpperInvariant();
            if (m.Groups[1].Value == "2025") de2025 ??= codigo;
            else de2021 ??= codigo;
        }

        if (de2025 is not null && CategoriasOwasp2025.Nombres.ContainsKey(de2025))
            return Crear(de2025, OrigenCategoriaOwasp.EtiquetaZap2025);

        if (de2021 is not null && TablaOwasp2021a2025.Mapa.TryGetValue(de2021, out var traducido))
            return Crear(traducido, OrigenCategoriaOwasp.EtiquetaZap2021);

        if (cweId is { } cwe && TablaCweOwasp2025.Mapa.TryGetValue(cwe, out var porCwe))
            return Crear(porCwe, OrigenCategoriaOwasp.TablaCwe);

        return Crear(CategoriasOwasp2025.PorDefecto, OrigenCategoriaOwasp.PorDefecto);
    }

    public static string RecomendacionPara(CategoriaOwasp categoria) =>
        RecomendacionesPorCategoria.Textos[categoria.Codigo];

    private static CategoriaOwasp Crear(string codigo, OrigenCategoriaOwasp origen) =>
        new(codigo, CategoriasOwasp2025.Nombres[codigo], origen);
}
