using Egida.Domain.Comun;

namespace Egida.Domain.Alertas;

public enum EstadoEnvio
{
    Pendiente,
    Enviado,
    Fallido,
}

/// <summary>
/// Aviso de un escaneo terminado con ≥ 1 hallazgo Alto (FR-027). Una por escaneo.
/// El destinatario es siempre el propietario del activo.
/// </summary>
public class Alerta
{
    public const int MaxIntentos = 3;

    /// <summary>Backoff entre intentos (R10): 10 s, 30 s, 90 s.</summary>
    public static readonly TimeSpan[] Esperas = [TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(90)];

    public Guid Id { get; private set; }
    public Guid EscaneoId { get; private set; }
    public Guid DestinatarioId { get; private set; }
    public string CorreoDestino { get; private set; } = null!;
    public DateTimeOffset CreadaEn { get; private set; }
    public int TotalAltas { get; private set; }
    public int TotalPosiblesFp { get; private set; }
    public string Resumen { get; private set; } = null!;
    public EstadoEnvio EstadoEnvio { get; private set; }
    public int IntentosEnvio { get; private set; }
    public DateTimeOffset? ProximoIntentoEn { get; private set; }
    public string? UltimoError { get; private set; }
    public DateTimeOffset? EnviadaEn { get; private set; }

    private Alerta() { }

    public Alerta(Guid escaneoId, Guid destinatarioId, string correoDestino, int totalAltas,
        int totalPosiblesFp, string resumen, DateTimeOffset ahora)
    {
        if (totalAltas < 1)
            throw new ExcepcionDominio("alerta_invalida", "Solo se genera una alerta con al menos un hallazgo Alto.");

        Id = Guid.CreateVersion7();
        EscaneoId = escaneoId;
        DestinatarioId = destinatarioId;
        CorreoDestino = correoDestino;
        TotalAltas = totalAltas;
        TotalPosiblesFp = totalPosiblesFp;
        Resumen = resumen.Length <= 1000 ? resumen : resumen[..1000];
        CreadaEn = ahora;
        EstadoEnvio = EstadoEnvio.Pendiente;
        ProximoIntentoEn = ahora;
    }

    public void RegistrarEnvioExitoso(DateTimeOffset ahora)
    {
        IntentosEnvio++;
        EstadoEnvio = EstadoEnvio.Enviado;
        EnviadaEn = ahora;
        ProximoIntentoEn = null;
        UltimoError = null;
    }

    public void RegistrarFalloEnvio(string error, DateTimeOffset ahora)
    {
        IntentosEnvio++;
        UltimoError = error.Length <= 500 ? error : error[..500];
        if (IntentosEnvio >= MaxIntentos)
        {
            EstadoEnvio = EstadoEnvio.Fallido;
            ProximoIntentoEn = null;
            return;
        }
        ProximoIntentoEn = ahora + Esperas[IntentosEnvio - 1];
    }
}
