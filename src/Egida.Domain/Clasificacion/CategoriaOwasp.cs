namespace Egida.Domain.Clasificacion;

/// <summary>Trazabilidad de la regla que asignó la categoría (research R3).</summary>
public enum OrigenCategoriaOwasp
{
    EtiquetaZap2025,
    EtiquetaZap2021,
    TablaCwe,
    PorDefecto,
}

/// <summary>Categoría OWASP Top 10:2025 con formato "A0X:2025 - Nombre".</summary>
public sealed record CategoriaOwasp(string Codigo, string Nombre, OrigenCategoriaOwasp Origen)
{
    public string Texto => $"{Codigo}:2025 - {Nombre}";
}

/// <summary>OWASP Top 10:2025 (versión vigente para la tesis).</summary>
public static class CategoriasOwasp2025
{
    public static readonly IReadOnlyDictionary<string, string> Nombres = new Dictionary<string, string>
    {
        ["A01"] = "Broken Access Control",
        ["A02"] = "Security Misconfiguration",
        ["A03"] = "Software Supply Chain Failures",
        ["A04"] = "Cryptographic Failures",
        ["A05"] = "Injection",
        ["A06"] = "Insecure Design",
        ["A07"] = "Authentication Failures",
        ["A08"] = "Software or Data Integrity Failures",
        ["A09"] = "Security Logging and Alerting Failures",
        ["A10"] = "Mishandling of Exceptional Conditions",
    };

    /// <summary>Categoría por defecto cuando ninguna otra regla aplica.</summary>
    public const string PorDefecto = "A02";
}

/// <summary>Traducción OWASP Top 10:2021 → 2025.</summary>
public static class TablaOwasp2021a2025
{
    public static readonly IReadOnlyDictionary<string, string> Mapa = new Dictionary<string, string>
    {
        ["A01"] = "A01", // Broken Access Control
        ["A02"] = "A04", // Cryptographic Failures
        ["A03"] = "A05", // Injection
        ["A04"] = "A06", // Insecure Design
        ["A05"] = "A02", // Security Misconfiguration
        ["A06"] = "A03", // Vulnerable and Outdated Components → Supply Chain
        ["A07"] = "A07", // Identification and Authentication Failures
        ["A08"] = "A08", // Software and Data Integrity Failures
        ["A09"] = "A09", // Security Logging and Monitoring Failures
        ["A10"] = "A01", // SSRF se integra en Broken Access Control
    };
}

/// <summary>CWE → OWASP Top 10:2025 para alertas sin etiquetas OWASP.</summary>
public static class TablaCweOwasp2025
{
    public static readonly IReadOnlyDictionary<int, string> Mapa = new Dictionary<int, string>
    {
        // A01 Broken Access Control
        [22] = "A01", [23] = "A01", [200] = "A01", [201] = "A01", [284] = "A01", [285] = "A01",
        [352] = "A01", [359] = "A01", [497] = "A01", [538] = "A01", [548] = "A01", [639] = "A01",
        [918] = "A01", [1275] = "A01",
        // A02 Security Misconfiguration
        [16] = "A02", [525] = "A02", [565] = "A02", [614] = "A02", [693] = "A02", [1004] = "A02",
        [1021] = "A02", [942] = "A02", [345] = "A08",
        // A03 Supply chain
        [829] = "A03", [1104] = "A03", [937] = "A03", [1035] = "A03",
        // A04 Cryptographic Failures
        [261] = "A04", [311] = "A04", [319] = "A04", [326] = "A04", [327] = "A04", [328] = "A04",
        // A05 Injection
        [20] = "A05", [74] = "A05", [77] = "A05", [78] = "A05", [79] = "A05", [80] = "A05",
        [89] = "A05", [90] = "A05", [91] = "A05", [94] = "A05", [113] = "A05", [116] = "A05",
        [643] = "A05", [917] = "A05", [943] = "A05",
        // A06 Insecure Design
        [209] = "A10", [256] = "A06", [501] = "A06", [522] = "A06", [602] = "A06",
        // A07 Authentication Failures
        [287] = "A07", [384] = "A07", [521] = "A07", [613] = "A07", [798] = "A07",
        // A08 Integrity
        [502] = "A08", [494] = "A08",
        // A09 Logging
        [117] = "A09", [223] = "A09", [532] = "A09", [778] = "A09",
        // A10 Exceptional conditions
        [248] = "A10", [390] = "A10", [391] = "A10", [754] = "A10", [755] = "A10",
    };
}

/// <summary>Recomendación genérica en español cuando el escáner no trae una.</summary>
public static class RecomendacionesPorCategoria
{
    public static readonly IReadOnlyDictionary<string, string> Textos = new Dictionary<string, string>
    {
        ["A01"] = "Aplique control de acceso en el servidor, denegando por defecto y validando la propiedad de cada recurso.",
        ["A02"] = "Revise la configuración del servidor y de la aplicación: cabeceras de seguridad, opciones por defecto y mensajes de error.",
        ["A03"] = "Mantenga un inventario de dependencias y actualice los componentes con vulnerabilidades conocidas.",
        ["A04"] = "Use TLS en todas las comunicaciones y algoritmos criptográficos actuales para proteger datos sensibles.",
        ["A05"] = "Valide y codifique toda entrada del usuario; use consultas parametrizadas y codificación de salida según el contexto.",
        ["A06"] = "Revise el diseño de la funcionalidad con modelado de amenazas y añada controles de negocio.",
        ["A07"] = "Refuerce la autenticación: límite de intentos, sesiones seguras y contraseñas robustas.",
        ["A08"] = "Verifique la integridad del software y de los datos (firmas, SRI) y evite deserializar datos no confiables.",
        ["A09"] = "Registre los eventos de seguridad y configure alertas para detectar ataques a tiempo.",
        ["A10"] = "Maneje las excepciones de forma segura, sin revelar detalles internos en los mensajes de error.",
    };
}
