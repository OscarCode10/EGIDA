using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Auditoria;
using Egida.Domain.Escaneos;
using Egida.Domain.Usuarios;
using Egida.Application.Escaneos;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Abstracciones;

public interface IReloj
{
    DateTimeOffset Ahora { get; }
}

/// <summary>
/// Unidad de trabajo sobre la base de datos. Application solo conoce las abstracciones de EF Core;
/// el proveedor (Npgsql) vive en Infrastructure.
/// </summary>
public interface IEgidaDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Activo> Activos { get; }
    DbSet<Escaneo> Escaneos { get; }
    DbSet<Hallazgo> Hallazgos { get; }
    DbSet<Alerta> Alertas { get; }
    DbSet<RegistroAuditoria> RegistrosAuditoria { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IUsuarioActual
{
    Guid Id { get; }
    Rol Rol { get; }
    bool EsAdministrador => Rol == Rol.Administrador;
    string? IpOrigen { get; }
}

public interface IHasherContrasenas
{
    string Hash(Usuario usuario, string contrasena);
    bool Verificar(Usuario usuario, string contrasena);
}

public interface IGeneradorTokens
{
    (string Token, DateTimeOffset ExpiraEn) Generar(Usuario usuario);
}

public interface IListaBlancaObjetivos
{
    IReadOnlyList<ObjetivoPermitido> Objetivos { get; }

    /// <summary>Devuelve el objetivo cuyo origen coincide exactamente, o null (FR-009, FR-013).</summary>
    ObjetivoPermitido? Buscar(UrlNormalizada url);
}

public interface IColaEscaneos
{
    /// <summary>Toma el escaneo pendiente más antiguo y lo pasa a EnCurso en la misma transacción.</summary>
    Task<Guid?> TomarSiguienteAsync(CancellationToken ct);

    /// <summary>Marca como Fallido ("Interrumpido") los escaneos que quedaron EnCurso.</summary>
    Task<IReadOnlyList<Guid>> MarcarInterrumpidosAsync(CancellationToken ct);
}

public interface IEscaner
{
    /// <summary>Ejecuta el perfil demo sobre el objetivo y devuelve hallazgos normalizados.</summary>
    Task<IReadOnlyList<DatosHallazgo>> EscanearAsync(ObjetivoPermitido objetivo, string urlActivo, CancellationToken ct);
}

public sealed record ResultadoIA(Guid HallazgoId, PrioridadIA Prioridad, decimal ProbabilidadFalsoPositivo, string Justificacion);

public sealed record RespuestaIA(string VersionModelo, IReadOnlyList<ResultadoIA> Resultados);

public interface IAnalizadorIA
{
    /// <summary>Null si la IA no está disponible o la respuesta no cumple el contrato (FR-024).</summary>
    Task<RespuestaIA?> AnalizarAsync(Guid escaneoId, IReadOnlyList<Hallazgo> hallazgos, CancellationToken ct);

    Task<bool> DisponibleAsync(CancellationToken ct);
}

public sealed record MensajeCorreo(string Para, string Asunto, string CuerpoHtml, string CuerpoTexto);

public interface IEnviadorCorreo
{
    Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct);
}

public static class TiposEvento
{
    public const string EscaneoActualizado = nameof(EscaneoActualizado);
    public const string HallazgosAnalizados = nameof(HallazgosAnalizados);
    public const string AlertaCreada = nameof(AlertaCreada);
    public const string AlertaEnvioActualizado = nameof(AlertaEnvioActualizado);
}

public interface IPublicadorEventos
{
    /// <summary>Publica un aviso de invalidación para el propietario del activo y los administradores.</summary>
    Task PublicarAsync(string tipo, Guid propietarioId, object datos, CancellationToken ct = default);
}
