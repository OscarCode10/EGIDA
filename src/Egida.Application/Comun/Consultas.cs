using Egida.Application.Abstracciones;
using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Comun;

/// <summary>
/// Alcance por rol (FR-002 a FR-004): el Cliente solo ve lo suyo; el Administrador, todo.
/// </summary>
public static class FiltroAlcance
{
    public static IQueryable<Activo> ActivosVisibles(this IEgidaDbContext db, IUsuarioActual usuario) =>
        usuario.EsAdministrador ? db.Activos : db.Activos.Where(a => a.PropietarioId == usuario.Id);

    public static IQueryable<Escaneo> EscaneosVisibles(this IEgidaDbContext db, IUsuarioActual usuario) =>
        usuario.EsAdministrador
            ? db.Escaneos
            : db.Escaneos.Where(e => db.Activos.Any(a => a.Id == e.ActivoId && a.PropietarioId == usuario.Id));

    public static IQueryable<Alerta> AlertasVisibles(this IEgidaDbContext db, IUsuarioActual usuario) =>
        usuario.EsAdministrador ? db.Alertas : db.Alertas.Where(a => a.DestinatarioId == usuario.Id);
}

/// <summary>Proyecciones compartidas a DTO.</summary>
public static class Consultas
{
    public static async Task<List<EscaneoDto>> ProyectarEscaneosAsync(IEgidaDbContext db,
        IQueryable<Escaneo> escaneos, int limite, CancellationToken ct)
    {
        var filas = await (
                from e in escaneos
                join a in db.Activos on e.ActivoId equals a.Id
                join p in db.Usuarios on a.PropietarioId equals p.Id
                join s in db.Usuarios on e.SolicitadoPorId equals s.Id
                orderby e.SolicitadoEn descending
                select new { e, a, p, s })
            .AsNoTracking()
            .Take(limite)
            .ToListAsync(ct);

        var ids = filas.Select(f => f.e.Id).ToList();
        var conteos = await db.Hallazgos.AsNoTracking()
            .Where(h => ids.Contains(h.EscaneoId))
            .GroupBy(h => new { h.EscaneoId, h.Severidad })
            .Select(g => new { g.Key.EscaneoId, g.Key.Severidad, Total = g.Count() })
            .ToListAsync(ct);
        var alertas = await db.Alertas.AsNoTracking()
            .Where(al => ids.Contains(al.EscaneoId))
            .Select(al => new { al.Id, al.EscaneoId, al.EstadoEnvio })
            .ToListAsync(ct);

        return filas.Select(f =>
        {
            int Contar(Severidad s) => conteos.Where(c => c.EscaneoId == f.e.Id && c.Severidad == s).Sum(c => c.Total);
            var alerta = alertas.FirstOrDefault(al => al.EscaneoId == f.e.Id);
            return new EscaneoDto(
                f.e.Id,
                new ActivoResumenDto(f.a.Id, f.a.Nombre, f.a.Url, new PersonaDto(f.p.Id, f.p.Nombre)),
                f.e.Estado.ToString(), f.e.SolicitadoEn, f.e.IniciadoEn, f.e.FinalizadoEn, f.e.MotivoFallo,
                f.e.TotalHallazgos,
                new ConteoSeveridadDto(Contar(Severidad.Alta), Contar(Severidad.Media), Contar(Severidad.Baja),
                    Contar(Severidad.Informativa)),
                new SolicitanteDto(f.s.Id, f.s.Nombre, f.s.Rol.ToString()),
                alerta is null ? null : new AlertaMinDto(alerta.Id, alerta.EstadoEnvio.ToString()));
        }).ToList();
    }

    public static async Task<List<AlertaDto>> ProyectarAlertasAsync(IEgidaDbContext db,
        IQueryable<Alerta> alertas, int limite, CancellationToken ct)
    {
        var filas = await (
                from al in alertas
                join e in db.Escaneos on al.EscaneoId equals e.Id
                join a in db.Activos on e.ActivoId equals a.Id
                join d in db.Usuarios on al.DestinatarioId equals d.Id
                orderby al.CreadaEn descending
                select new { al, a, d })
            .AsNoTracking()
            .Take(limite)
            .ToListAsync(ct);

        return filas.Select(f => new AlertaDto(f.al.Id, f.al.EscaneoId, new ActivoMinDto(f.a.Id, f.a.Nombre),
            f.al.CreadaEn, f.al.TotalAltas, f.al.TotalPosiblesFp, f.al.Resumen, f.al.EstadoEnvio.ToString(),
            new PersonaDto(f.d.Id, f.d.Nombre))).ToList();
    }

    public static HallazgoDto AHallazgoDto(Hallazgo h) => new(
        h.Id, h.Nombre, h.Descripcion, h.Severidad.ToString(), h.CategoriaOwasp, h.OrigenCategoria.ToString(),
        h.UrlAfectada, h.Recomendacion, h.MetodoHttp, h.Parametro, h.Evidencia, h.CweId, h.Ocurrencias,
        h.EstadoIA == EstadoAnalisisIA.Analizado
            ? new IaDto("Analizado", h.PrioridadIA?.ToString(), h.ProbabilidadFalsoPositivo,
                h.EsPosibleFalsoPositivo, h.JustificacionIA, h.VersionModeloIA)
            : new IaDto("Pendiente", null, null, null, null, null));
}
