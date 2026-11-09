using Egida.Application.Abstracciones;
using Egida.Application.Alertas;
using Egida.Application.AnalisisIA;
using Egida.Domain.Activos;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Egida.Application.Escaneos;

/// <summary>
/// Caso de uso EjecutarEscaneo (lado Worker). El escaneo ya viene EnCurso desde la cola.
/// Flujo: re-verificar lista blanca → escanear → guardar hallazgos → IA → Terminado + Alerta.
/// La falla de la IA o del correo nunca marca el escaneo como Fallido.
/// </summary>
public sealed class EjecutorEscaneos(
    IEgidaDbContext db, IListaBlancaObjetivos listaBlanca, IEscaner escaner, ServicioAnalisisIA analisisIA,
    GeneradorAlertas alertas, IPublicadorEventos eventos, IReloj reloj, IOptions<OpcionesEscaneo> opciones,
    ILogger<EjecutorEscaneos> log)
{
    public async Task EjecutarAsync(Guid escaneoId, CancellationToken ct)
    {
        var escaneo = await db.Escaneos.FirstAsync(e => e.Id == escaneoId, ct);
        var activo = await db.Activos.FirstAsync(a => a.Id == escaneo.ActivoId, ct);
        if (escaneo.Estado == EstadoEscaneo.Pendiente) escaneo.Iniciar(reloj.Ahora);
        await db.SaveChangesAsync(ct);
        await PublicarEstadoAsync(escaneo, activo, ct);

        // FR-013: se vuelve a verificar la lista blanca al ejecutar.
        var objetivo = listaBlanca.Buscar(UrlNormalizada.Crear(activo.Url));
        if (objetivo is null)
        {
            await FallarAsync(escaneo, activo, MotivosFallo.ObjetivoNoAutorizado, ct);
            return;
        }

        IReadOnlyList<DatosHallazgo> datos;
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limite.CancelAfter(TimeSpan.FromMinutes(opciones.Value.TiempoMaximoMinutos));
        try
        {
            log.LogInformation("Escaneando {Url} (escaneo {Id})", activo.Url, escaneo.Id);
            datos = await escaner.EscanearAsync(objetivo, activo.Url, limite.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            await FallarAsync(escaneo, activo, MotivosFallo.TiempoExcedido, ct);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Falló el escaneo {Id}", escaneo.Id);
            await FallarAsync(escaneo, activo, $"{MotivosFallo.ObjetivoNoDisponible}: {ex.Message}", ct);
            return;
        }

        var hallazgos = new List<Hallazgo>();
        foreach (var d in datos)
        {
            try
            {
                hallazgos.Add(new Hallazgo(escaneo.Id, d));
            }
            catch (Domain.Comun.ExcepcionDominio ex)
            {
                // Principio III: un hallazgo incompleto no se guarda como válido.
                log.LogWarning("Hallazgo descartado ({Motivo}): {Nombre}", ex.Message, d.Nombre);
            }
        }
        db.Hallazgos.AddRange(hallazgos);
        await db.SaveChangesAsync(ct);

        // IA (FR-022/FR-024): nunca bloquea el flujo.
        var (analizados, pendientes) = await analisisIA.AnalizarAsync(escaneo.Id, hallazgos, ct);

        escaneo.Terminar(hallazgos.Count, reloj.Ahora);
        var alerta = await alertas.CrearSiCorrespondeAsync(escaneo, activo, hallazgos, ct);
        await db.SaveChangesAsync(ct);

        log.LogInformation("Escaneo {Id} terminado: {Total} hallazgos, {Altas} altos", escaneo.Id, hallazgos.Count,
            hallazgos.Count(h => h.Severidad == Severidad.Alta));
        await PublicarEstadoAsync(escaneo, activo, ct);
        await eventos.PublicarAsync(TiposEvento.HallazgosAnalizados, activo.PropietarioId,
            new { escaneoId = escaneo.Id, analizados, pendientes }, ct);
        if (alerta is not null)
            await eventos.PublicarAsync(TiposEvento.AlertaCreada, activo.PropietarioId,
                new { alertaId = alerta.Id, escaneoId = escaneo.Id, totalAltas = alerta.TotalAltas }, ct);
    }

    private async Task FallarAsync(Escaneo escaneo, Activo activo, string motivo, CancellationToken ct)
    {
        escaneo.Fallar(motivo, reloj.Ahora);
        await db.SaveChangesAsync(ct);
        await PublicarEstadoAsync(escaneo, activo, ct);
    }

    private Task PublicarEstadoAsync(Escaneo e, Activo a, CancellationToken ct) =>
        eventos.PublicarAsync(TiposEvento.EscaneoActualizado, a.PropietarioId,
            new { escaneoId = e.Id, activoId = a.Id, estado = e.Estado.ToString(), motivoFallo = e.MotivoFallo, totalHallazgos = e.TotalHallazgos }, ct);
}
