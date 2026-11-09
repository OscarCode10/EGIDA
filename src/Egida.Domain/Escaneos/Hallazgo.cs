using Egida.Domain.Clasificacion;
using Egida.Domain.Comun;

namespace Egida.Domain.Escaneos;

/// <summary>Taxonomía de 4 niveles (constitución v1.0.1, principio III). Desde el riesgo de ZAP.</summary>
public enum Severidad
{
    Informativa = 0,
    Baja = 1,
    Media = 2,
    Alta = 3,
}

/// <summary>Se rotula siempre "Prioridad IA" en la interfaz para no confundirla con la severidad.</summary>
public enum PrioridadIA
{
    Baja = 1,
    Media = 2,
    Alta = 3,
}

public enum EstadoAnalisisIA
{
    Pendiente,
    Analizado,
}

/// <summary>Datos de entrada para construir un hallazgo (ya normalizados).</summary>
public sealed record DatosHallazgo(
    string Nombre,
    string Descripcion,
    Severidad? Severidad,
    CategoriaOwasp? Categoria,
    string UrlAfectada,
    string Recomendacion,
    int PluginId,
    int ConfianzaEscaner,
    string? MetodoHttp = null,
    string? Parametro = null,
    string? Evidencia = null,
    int? CweId = null,
    int Ocurrencias = 1,
    string? TipoContenido = null);

public class Hallazgo
{
    public const decimal UmbralFalsoPositivo = 0.5m;

    public Guid Id { get; private set; }
    public Guid EscaneoId { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Descripcion { get; private set; } = null!;

    /// <summary>Nunca cambia después de crear el hallazgo (FR-026).</summary>
    public Severidad Severidad { get; private set; }

    public string CategoriaOwasp { get; private set; } = null!;
    public OrigenCategoriaOwasp OrigenCategoria { get; private set; }
    public string UrlAfectada { get; private set; } = null!;
    public string Recomendacion { get; private set; } = null!;
    public string? MetodoHttp { get; private set; }
    public string? Parametro { get; private set; }
    public string? Evidencia { get; private set; }
    public string? TipoContenido { get; private set; }
    public int? CweId { get; private set; }
    public int PluginId { get; private set; }
    public int ConfianzaEscaner { get; private set; }
    public int Ocurrencias { get; private set; }

    public EstadoAnalisisIA EstadoIA { get; private set; }
    public PrioridadIA? PrioridadIA { get; private set; }
    public decimal? ProbabilidadFalsoPositivo { get; private set; }
    public string? JustificacionIA { get; private set; }
    public string? VersionModeloIA { get; private set; }
    public DateTimeOffset? AnalizadoEn { get; private set; }
    public int IntentosIA { get; private set; }

    /// <summary>FR-023: posible falso positivo si la probabilidad es ≥ 50 %.</summary>
    public bool EsPosibleFalsoPositivo => ProbabilidadFalsoPositivo >= UmbralFalsoPositivo;

    private Hallazgo() { }

    public Hallazgo(Guid escaneoId, DatosHallazgo datos)
    {
        // Principio III: no existe un hallazgo sin severidad ni categoría OWASP.
        if (datos.Severidad is null)
            throw new ExcepcionDominio("hallazgo_incompleto", "Un hallazgo debe tener severidad.");
        if (datos.Categoria is null)
            throw new ExcepcionDominio("hallazgo_incompleto", "Un hallazgo debe tener categoría OWASP Top 10.");
        if (string.IsNullOrWhiteSpace(datos.Nombre))
            throw new ExcepcionDominio("hallazgo_incompleto", "Un hallazgo debe tener nombre.");
        if (string.IsNullOrWhiteSpace(datos.UrlAfectada))
            throw new ExcepcionDominio("hallazgo_incompleto", "Un hallazgo debe tener URL afectada.");
        if (string.IsNullOrWhiteSpace(datos.Recomendacion))
            throw new ExcepcionDominio("hallazgo_incompleto", "Un hallazgo debe tener recomendación.");
        if (datos.ConfianzaEscaner is < 1 or > 3)
            throw new ExcepcionDominio("hallazgo_incompleto", "La confianza del escáner debe estar entre 1 y 3.");

        Id = Guid.CreateVersion7();
        EscaneoId = escaneoId;
        Nombre = Recortar(datos.Nombre.Trim(), 300)!;
        Descripcion = string.IsNullOrWhiteSpace(datos.Descripcion) ? Nombre : datos.Descripcion.Trim();
        Severidad = datos.Severidad.Value;
        CategoriaOwasp = datos.Categoria.Texto;
        OrigenCategoria = datos.Categoria.Origen;
        UrlAfectada = Recortar(datos.UrlAfectada, 2048)!;
        Recomendacion = datos.Recomendacion.Trim();
        MetodoHttp = Recortar(datos.MetodoHttp, 10);
        Parametro = Recortar(Vacio(datos.Parametro), 300);
        Evidencia = Recortar(Vacio(datos.Evidencia), 2000);
        TipoContenido = Recortar(Vacio(datos.TipoContenido), 100);
        CweId = datos.CweId is > 0 ? datos.CweId : null;
        PluginId = datos.PluginId;
        ConfianzaEscaner = datos.ConfianzaEscaner;
        Ocurrencias = Math.Max(1, datos.Ocurrencias);
        EstadoIA = EstadoAnalisisIA.Pendiente;
    }

    public void RegistrarAnalisisIA(PrioridadIA prioridad, decimal probabilidadFalsoPositivo,
        string justificacion, string? versionModelo, DateTimeOffset ahora)
    {
        if (probabilidadFalsoPositivo is < 0m or > 1m)
            throw new ExcepcionDominio("ia_invalida", "La probabilidad de falso positivo debe estar entre 0 y 1.");
        if (!Enum.IsDefined(prioridad))
            throw new ExcepcionDominio("ia_invalida", "Prioridad IA desconocida.");
        if (string.IsNullOrWhiteSpace(justificacion))
            throw new ExcepcionDominio("ia_invalida", "La IA debe justificar su resultado.");

        PrioridadIA = prioridad;
        ProbabilidadFalsoPositivo = Math.Round(probabilidadFalsoPositivo, 4);
        JustificacionIA = Recortar(justificacion.Trim(), 500);
        VersionModeloIA = Recortar(versionModelo, 40);
        AnalizadoEn = ahora;
        EstadoIA = EstadoAnalisisIA.Analizado;
        IntentosIA++;
    }

    /// <summary>FR-024: la IA no respondió; el hallazgo queda "análisis IA pendiente".</summary>
    public void RegistrarFalloIA()
    {
        if (EstadoIA == EstadoAnalisisIA.Pendiente) IntentosIA++;
    }

    private static string? Vacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string? Recortar(string? valor, int max) =>
        valor is null ? null : valor.Length <= max ? valor : valor[..max];
}
