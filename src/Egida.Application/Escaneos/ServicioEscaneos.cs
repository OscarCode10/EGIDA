using Egida.Application.Abstracciones;
using Egida.Application.Activos;
using Egida.Application.Comun;
using Egida.Domain.Activos;
using Egida.Domain.Auditoria;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Escaneos;

/// <summary>Casos de uso SolicitarEscaneo, ListarEscaneos y ConsultarHallazgos (lado API).</summary>
public sealed class ServicioEscaneos(
    IEgidaDbContext db, IUsuarioActual usuario, ServicioActivos activos, IListaBlancaObjetivos listaBlanca,
    IPublicadorEventos eventos, IReloj reloj)
{
    public const string MensajeEnCurso = "Ya hay un escaneo en marcha para este activo.";

    /// <summary>FR-012: responde de inmediato dejando el escaneo Pendiente; el Worker lo ejecuta.</summary>
    public async Task<EscaneoSolicitadoDto> SolicitarAsync(Guid activoId, CancellationToken ct = default)
    {
        var activo = await activos.ObtenerVisibleAsync(activoId, ct);

        if (listaBlanca.Buscar(UrlNormalizada.Crear(activo.Url)) is null)
            throw new ErrorAplicacion(TipoError.Validacion, "objetivo_no_autorizado", ServicioActivos.MensajeNoAutorizado);

        if (await db.Escaneos.AnyAsync(e => e.ActivoId == activoId &&
                (e.Estado == EstadoEscaneo.Pendiente || e.Estado == EstadoEscaneo.EnCurso), ct))
            throw new ErrorAplicacion(TipoError.Conflicto, "escaneo_en_curso", MensajeEnCurso);

        var escaneo = new Escaneo(activoId, usuario.Id, reloj.Ahora);
        db.Escaneos.Add(escaneo);
        var accion = usuario.EsAdministrador && activo.PropietarioId != usuario.Id
            ? AccionesAuditoria.EscaneoSolicitadoPorAdmin
            : AccionesAuditoria.EscaneoSolicitado;
        db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuario.Id, accion, ResultadosAuditoria.Exito,
            recurso: $"escaneos/{escaneo.Id}", ipOrigen: usuario.IpOrigen));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // El índice único parcial garantiza FR-015 incluso con solicitudes simultáneas.
            throw new ErrorAplicacion(TipoError.Conflicto, "escaneo_en_curso", MensajeEnCurso);
        }

        await eventos.PublicarAsync(TiposEvento.EscaneoActualizado, activo.PropietarioId,
            new { escaneoId = escaneo.Id, activoId, estado = escaneo.Estado.ToString(), motivoFallo = (string?)null, totalHallazgos = 0 }, ct);

        var solicitante = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == usuario.Id, ct);
        return new EscaneoSolicitadoDto(escaneo.Id, activoId, escaneo.Estado.ToString(), escaneo.SolicitadoEn,
            new SolicitanteDto(solicitante.Id, solicitante.Nombre, solicitante.Rol.ToString()));
    }

    public async Task<IReadOnlyList<EscaneoDto>> ListarAsync(Guid? activoId, int? limite, CancellationToken ct = default)
    {
        var consulta = db.EscaneosVisibles(usuario);
        if (activoId is { } id) consulta = consulta.Where(e => e.ActivoId == id);
        return await Consultas.ProyectarEscaneosAsync(db, consulta, Math.Clamp(limite ?? 20, 1, 100), ct);
    }

    public async Task<EscaneoDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        await VerificarVisibleAsync(id, ct);
        var dto = (await Consultas.ProyectarEscaneosAsync(db, db.Escaneos.Where(e => e.Id == id), 1, ct)).Single();
        db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuario.Id,
            AccionesAuditoria.ResultadoEscaneoConsultado, ResultadosAuditoria.Exito, recurso: $"escaneos/{id}",
            ipOrigen: usuario.IpOrigen));
        await db.SaveChangesAsync(ct);
        return dto;
    }

    /// <summary>FR-021 y HU4-4: filtro por severidad y orden por severidad o por Prioridad IA.</summary>
    public async Task<IReadOnlyList<HallazgoDto>> HallazgosAsync(Guid escaneoId, string? severidad, string? orden,
        CancellationToken ct = default)
    {
        await VerificarVisibleAsync(escaneoId, ct);
        var consulta = db.Hallazgos.AsNoTracking().Where(h => h.EscaneoId == escaneoId);
        if (!string.IsNullOrWhiteSpace(severidad))
        {
            if (!Enum.TryParse<Severidad>(severidad, true, out var s))
                throw ErrorAplicacion.Validacion("severidad", "Use Alta, Media, Baja o Informativa.");
            consulta = consulta.Where(h => h.Severidad == s);
        }
        var lista = await consulta.ToListAsync(ct);
        return Ordenar(lista, orden).Select(Consultas.AHallazgoDto).ToList();
    }

    public static IEnumerable<Hallazgo> Ordenar(IEnumerable<Hallazgo> hallazgos, string? orden) =>
        string.Equals(orden, "prioridadIA", StringComparison.OrdinalIgnoreCase)
            ? hallazgos.OrderByDescending(h => (int?)h.PrioridadIA ?? 0).ThenByDescending(h => h.Severidad)
                .ThenBy(h => h.Nombre)
            : hallazgos.OrderByDescending(h => h.Severidad).ThenByDescending(h => (int?)h.PrioridadIA ?? 0)
                .ThenBy(h => h.Nombre);

    private async Task VerificarVisibleAsync(Guid escaneoId, CancellationToken ct)
    {
        if (await db.EscaneosVisibles(usuario).AnyAsync(e => e.Id == escaneoId, ct)) return;
        if (await db.Escaneos.AnyAsync(e => e.Id == escaneoId, ct))
        {
            db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuario.Id, AccionesAuditoria.AccesoDenegado,
                ResultadosAuditoria.Denegado, recurso: $"escaneos/{escaneoId}", ipOrigen: usuario.IpOrigen));
            await db.SaveChangesAsync(ct);
        }
        throw ErrorAplicacion.NoEncontrado("el escaneo");
    }
}
