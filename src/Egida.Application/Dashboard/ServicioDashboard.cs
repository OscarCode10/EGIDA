using Egida.Application.Abstracciones;
using Egida.Application.Comun;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Dashboard;

/// <summary>Caso de uso ObtenerResumen (FR-031): alcance propio para el Cliente, global para el Administrador.</summary>
public sealed class ServicioDashboard(IEgidaDbContext db, IUsuarioActual usuario)
{
    public const int Recientes = 5;

    public async Task<DashboardDto> ObtenerAsync(CancellationToken ct = default)
    {
        var totalActivos = await db.ActivosVisibles(usuario).CountAsync(ct);
        var escaneos = await Consultas.ProyectarEscaneosAsync(db, db.EscaneosVisibles(usuario), Recientes, ct);

        var idsVisibles = db.EscaneosVisibles(usuario).Select(e => e.Id);
        var conteos = await db.Hallazgos.AsNoTracking()
            .Where(h => idsVisibles.Contains(h.EscaneoId))
            .GroupBy(h => h.Severidad)
            .Select(g => new { Severidad = g.Key, Total = g.Count() })
            .ToListAsync(ct);
        int Contar(Severidad s) => conteos.Where(c => c.Severidad == s).Sum(c => c.Total);

        var alertas = await Consultas.ProyectarAlertasAsync(db, db.AlertasVisibles(usuario), Recientes, ct);

        return new DashboardDto(totalActivos, escaneos,
            new ConteoSeveridadDto(Contar(Severidad.Alta), Contar(Severidad.Media), Contar(Severidad.Baja),
                Contar(Severidad.Informativa)),
            alertas);
    }
}
