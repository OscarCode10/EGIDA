using Egida.Domain.Comun;

namespace Egida.Domain.Escaneos;

public enum EstadoEscaneo
{
    Pendiente,
    EnCurso,
    Terminado,
    Fallido,
}

public static class MotivosFallo
{
    public const string TiempoExcedido = "Tiempo excedido";
    public const string Interrumpido = "Interrumpido";
    public const string ObjetivoNoDisponible = "Objetivo no disponible";
    public const string ObjetivoNoAutorizado = "Objetivo ya no autorizado";
}

/// <summary>Máquina de estados: Pendiente → EnCurso → Terminado | Fallido (ver data-model.md).</summary>
public class Escaneo
{
    public Guid Id { get; private set; }
    public Guid ActivoId { get; private set; }
    public Guid SolicitadoPorId { get; private set; }
    public EstadoEscaneo Estado { get; private set; }
    public DateTimeOffset SolicitadoEn { get; private set; }
    public DateTimeOffset? IniciadoEn { get; private set; }
    public DateTimeOffset? FinalizadoEn { get; private set; }
    public string? MotivoFallo { get; private set; }
    public int TotalHallazgos { get; private set; }

    private Escaneo() { }

    public Escaneo(Guid activoId, Guid solicitadoPorId, DateTimeOffset ahora)
    {
        Id = Guid.CreateVersion7();
        ActivoId = activoId;
        SolicitadoPorId = solicitadoPorId;
        Estado = EstadoEscaneo.Pendiente;
        SolicitadoEn = ahora;
    }

    public bool EsFinal => Estado is EstadoEscaneo.Terminado or EstadoEscaneo.Fallido;

    public void Iniciar(DateTimeOffset ahora)
    {
        if (Estado != EstadoEscaneo.Pendiente)
            throw new ExcepcionDominio("transicion_invalida", $"No se puede iniciar un escaneo en estado {Estado}.");
        Estado = EstadoEscaneo.EnCurso;
        IniciadoEn = ahora;
    }

    public void Terminar(int totalHallazgos, DateTimeOffset ahora)
    {
        if (Estado != EstadoEscaneo.EnCurso)
            throw new ExcepcionDominio("transicion_invalida", $"No se puede terminar un escaneo en estado {Estado}.");
        ArgumentOutOfRangeException.ThrowIfNegative(totalHallazgos);
        Estado = EstadoEscaneo.Terminado;
        TotalHallazgos = totalHallazgos;
        FinalizadoEn = ahora;
    }

    public void Fallar(string motivo, DateTimeOffset ahora)
    {
        if (EsFinal)
            throw new ExcepcionDominio("transicion_invalida", $"El escaneo ya está en estado final {Estado}.");
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ExcepcionDominio("validacion", "Un escaneo fallido requiere un motivo.");
        Estado = EstadoEscaneo.Fallido;
        MotivoFallo = motivo.Length <= 500 ? motivo : motivo[..500];
        FinalizadoEn = ahora;
    }
}
