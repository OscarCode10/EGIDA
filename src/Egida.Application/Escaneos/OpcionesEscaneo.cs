namespace Egida.Application.Escaneos;

/// <summary>Objetivo autorizado (sección Egida:Escaneo:Objetivos). No se guarda en BD.</summary>
public sealed class ObjetivoPermitido
{
    public string Url { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Rutas que el escáner visita antes del spider (Juice Shop es una SPA, R1).</summary>
    public List<string> UrlsSemilla { get; set; } = [];
}

public sealed class OpcionesEscaneo
{
    public const string Seccion = "Egida:Escaneo";

    public List<ObjetivoPermitido> Objetivos { get; set; } = [];

    /// <summary>FR-016: tiempo máximo por escaneo.</summary>
    public int TiempoMaximoMinutos { get; set; } = 15;

    public int SegundosSpider { get; set; } = 60;
    public int SegundosEscaneoActivo { get; set; } = 120;
    public int SegundosPasivo { get; set; } = 60;
}
