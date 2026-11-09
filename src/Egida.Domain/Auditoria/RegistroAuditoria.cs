namespace Egida.Domain.Auditoria;

public static class AccionesAuditoria
{
    public const string LoginExitoso = nameof(LoginExitoso);
    public const string LoginFallido = nameof(LoginFallido);
    public const string CuentaBloqueada = nameof(CuentaBloqueada);
    public const string ActivoRegistrado = nameof(ActivoRegistrado);
    public const string EscaneoSolicitado = nameof(EscaneoSolicitado);
    public const string EscaneoSolicitadoPorAdmin = nameof(EscaneoSolicitadoPorAdmin);
    public const string ResultadoEscaneoConsultado = nameof(ResultadoEscaneoConsultado);
    public const string AccesoDenegado = nameof(AccesoDenegado);
}

public static class ResultadosAuditoria
{
    public const string Exito = nameof(Exito);
    public const string Fallo = nameof(Fallo);
    public const string Denegado = nameof(Denegado);
}

/// <summary>Evento de seguridad. Solo se inserta, nunca se modifica (principio I).</summary>
public class RegistroAuditoria
{
    public Guid Id { get; private set; }
    public DateTimeOffset Fecha { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string? CorreoIntentado { get; private set; }
    public string Accion { get; private set; } = null!;
    public string? Recurso { get; private set; }
    public string Resultado { get; private set; } = null!;
    public string? IpOrigen { get; private set; }

    private RegistroAuditoria() { }

    public RegistroAuditoria(DateTimeOffset fecha, Guid? usuarioId, string accion, string resultado,
        string? recurso = null, string? correoIntentado = null, string? ipOrigen = null)
    {
        Id = Guid.CreateVersion7();
        Fecha = fecha;
        UsuarioId = usuarioId;
        Accion = accion;
        Resultado = resultado;
        Recurso = Recortar(recurso, 200);
        CorreoIntentado = Recortar(correoIntentado, 254);
        IpOrigen = Recortar(ipOrigen, 45);
    }

    private static string? Recortar(string? valor, int max) =>
        valor is null ? null : valor.Length <= max ? valor : valor[..max];
}
