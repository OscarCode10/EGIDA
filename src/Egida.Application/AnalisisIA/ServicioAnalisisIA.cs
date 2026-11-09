using Egida.Application.Abstracciones;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Egida.Application.AnalisisIA;

/// <summary>Casos de uso AnalizarHallazgos y ReintentarPendientes (FR-022 a FR-025, R9).</summary>
public sealed class ServicioAnalisisIA(
    IEgidaDbContext db, IAnalizadorIA analizador, IPublicadorEventos eventos, IReloj reloj,
    ILogger<ServicioAnalisisIA> log)
{
    public const int TamanoLote = 200;

    /// <summary>Analiza por lotes; si un lote falla, los ya analizados se conservan y el resto queda Pendiente.</summary>
    public async Task<(int Analizados, int Pendientes)> AnalizarAsync(Guid escaneoId, IReadOnlyList<Hallazgo> hallazgos,
        CancellationToken ct)
    {
        foreach (var lote in hallazgos.Chunk(TamanoLote))
        {
            RespuestaIA? respuesta = null;
            try
            {
                respuesta = await analizador.AnalizarAsync(escaneoId, lote, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                log.LogWarning(ex, "La IA no respondió para el escaneo {Id}", escaneoId);
            }

            if (respuesta is null)
            {
                foreach (var h in lote) h.RegistrarFalloIA();
                continue;
            }

            var porId = respuesta.Resultados.ToDictionary(r => r.HallazgoId);
            foreach (var h in lote)
            {
                var r = porId[h.Id];
                h.RegistrarAnalisisIA(r.Prioridad, r.ProbabilidadFalsoPositivo, r.Justificacion,
                    respuesta.VersionModelo, reloj.Ahora);
            }
        }
        await db.SaveChangesAsync(ct);

        var analizados = hallazgos.Count(h => h.EstadoIA == EstadoAnalisisIA.Analizado);
        return (analizados, hallazgos.Count - analizados);
    }

    /// <summary>FR-025: reintenta los hallazgos pendientes de escaneos terminados si la IA está disponible.</summary>
    public async Task<int> ReintentarPendientesAsync(CancellationToken ct)
    {
        var escaneosConPendientes = await (
                from h in db.Hallazgos
                join e in db.Escaneos on h.EscaneoId equals e.Id
                where h.EstadoIA == EstadoAnalisisIA.Pendiente && e.Estado == EstadoEscaneo.Terminado
                select e.Id)
            .Distinct().ToListAsync(ct);
        if (escaneosConPendientes.Count == 0) return 0;
        if (!await analizador.DisponibleAsync(ct)) return 0;

        var total = 0;
        foreach (var escaneoId in escaneosConPendientes)
        {
            var pendientes = await db.Hallazgos
                .Where(h => h.EscaneoId == escaneoId && h.EstadoIA == EstadoAnalisisIA.Pendiente)
                .ToListAsync(ct);
            var (analizados, restantes) = await AnalizarAsync(escaneoId, pendientes, ct);
            total += analizados;

            var propietarioId = await (
                    from e in db.Escaneos
                    join a in db.Activos on e.ActivoId equals a.Id
                    where e.Id == escaneoId
                    select a.PropietarioId)
                .FirstAsync(ct);
            await eventos.PublicarAsync(TiposEvento.HallazgosAnalizados, propietarioId,
                new { escaneoId, analizados, pendientes = restantes }, ct);
        }
        return total;
    }
}
