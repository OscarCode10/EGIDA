namespace Egida.Application.Comun;

// DTO de la API REST (contracts/api-rest.md). Se serializan en camelCase.

public sealed record UsuarioDto(Guid Id, string Nombre, string Correo, string Rol);

public sealed record RespuestaLogin(string Token, DateTimeOffset ExpiraEn, UsuarioDto Usuario);

public sealed record ObjetivoDto(string Nombre, string Url);

public sealed record PersonaDto(Guid Id, string Nombre);

public sealed record UltimoEscaneoDto(Guid Id, string Estado, DateTimeOffset? FinalizadoEn);

public sealed record ActivoDto(Guid Id, string Nombre, string Url, PersonaDto Propietario,
    DateTimeOffset CreadoEn, UltimoEscaneoDto? UltimoEscaneo);

public sealed record SolicitanteDto(Guid Id, string Nombre, string Rol);

public sealed record EscaneoSolicitadoDto(Guid Id, Guid ActivoId, string Estado, DateTimeOffset SolicitadoEn,
    SolicitanteDto SolicitadoPor);

public sealed record ActivoResumenDto(Guid Id, string Nombre, string Url, PersonaDto Propietario);

public sealed record ConteoSeveridadDto(int Alta, int Media, int Baja, int Informativa)
{
    public static readonly ConteoSeveridadDto Cero = new(0, 0, 0, 0);
}

public sealed record AlertaMinDto(Guid Id, string EstadoEnvio);

public sealed record EscaneoDto(Guid Id, ActivoResumenDto Activo, string Estado, DateTimeOffset SolicitadoEn,
    DateTimeOffset? IniciadoEn, DateTimeOffset? FinalizadoEn, string? MotivoFallo, int TotalHallazgos,
    ConteoSeveridadDto ConteoPorSeveridad, SolicitanteDto SolicitadoPor, AlertaMinDto? Alerta);

public sealed record IaDto(string Estado, string? Prioridad, decimal? ProbabilidadFalsoPositivo,
    bool? EsPosibleFalsoPositivo, string? Justificacion, string? VersionModelo);

public sealed record HallazgoDto(Guid Id, string Nombre, string Descripcion, string Severidad,
    string CategoriaOwasp, string OrigenCategoria, string UrlAfectada, string Recomendacion,
    string? MetodoHttp, string? Parametro, string? Evidencia, int? CweId, int Ocurrencias, IaDto Ia);

public sealed record ActivoMinDto(Guid Id, string Nombre);

public sealed record AlertaDto(Guid Id, Guid EscaneoId, ActivoMinDto Activo, DateTimeOffset CreadaEn,
    int TotalAltas, int TotalPosiblesFp, string Resumen, string EstadoEnvio, PersonaDto Destinatario);

public sealed record DashboardDto(int TotalActivos, IReadOnlyList<EscaneoDto> EscaneosRecientes,
    ConteoSeveridadDto HallazgosPorSeveridad, IReadOnlyList<AlertaDto> AlertasRecientes);
