using Egida.Application.Abstracciones;
using Egida.Application.Alertas;
using Egida.Application.AnalisisIA;
using Egida.Application.Escaneos;

namespace Egida.Worker;

/// <summary>
/// Cola de escaneos (R4): al arrancar marca como "Interrumpido" lo que quedÃ³ EnCurso;
/// luego cada 3 s toma el siguiente pendiente y lo ejecuta (uno a la vez por instancia de ZAP).
/// </summary>
public sealed class ProcesadorEscaneos(IServiceScopeFactory scopes, IColaEscaneos cola, ILogger<ProcesadorEscaneos> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await EsperarBaseDatosAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            Guid? escaneoId = null;
            try
            {
                escaneoId = await cola.TomarSiguienteAsync(ct);
                if (escaneoId is { } id)
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<EjecutorEscaneos>().EjecutarAsync(id, ct);
                    continue;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error procesando el escaneo {Id}", escaneoId);
            }
            await Task.Delay(TimeSpan.FromSeconds(3), ct);
        }
    }

    private async Task EsperarBaseDatosAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var interrumpidos = await cola.MarcarInterrumpidosAsync(ct);
                if (interrumpidos.Count > 0)
                    log.LogWarning("{N} escaneo(s) quedaron interrumpidos por un reinicio", interrumpidos.Count);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("Base de datos no lista ({Error}); reintento en 3 s", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(3), ct);
            }
        }
    }
}

/// <summary>FR-025: cada 60 s reanaliza los hallazgos "anÃ¡lisis IA pendiente" si la IA responde.</summary>
public sealed class ReintentadorAnalisisIA(IServiceScopeFactory scopes, IConfiguration config,
    ILogger<ReintentadorAnalisisIA> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var intervalo = TimeSpan.FromSeconds(config.GetValue("IA:SegundosReintento", 60));
        using var temporizador = new PeriodicTimer(intervalo);
        while (await temporizador.WaitForNextTickAsync(ct))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var n = await scope.ServiceProvider.GetRequiredService<ServicioAnalisisIA>().ReintentarPendientesAsync(ct);
                if (n > 0) log.LogInformation("Reanalizados {N} hallazgos pendientes", n);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("Reintento de IA fallido: {Error}", ex.Message);
            }
        }
    }
}

/// <summary>Outbox de alertas (R10, FR-029): envÃ­a las pendientes por SMTP con hasta 3 intentos.</summary>
public sealed class EnviadorAlertas(IServiceScopeFactory scopes, ILogger<EnviadorAlertas> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await temporizador.WaitForNextTickAsync(ct))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ServicioEnvioAlertas>().EnviarPendientesAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                log.LogWarning("Error en el envÃ­o de alertas: {Error}", ex.Message);
            }
        }
    }
}
