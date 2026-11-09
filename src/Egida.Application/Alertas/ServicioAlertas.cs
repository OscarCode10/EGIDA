using System.Globalization;
using System.Net;
using System.Text;
using Egida.Application.Abstracciones;
using Egida.Application.Comun;
using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Escaneos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Egida.Application.Alertas;

public sealed class OpcionesAlertas
{
    /// <summary>URL con la que el navegador abre ÉGIDA (enlace del correo).</summary>
    public string UrlPublica { get; set; } = "http://localhost:5173";
}

/// <summary>Caso de uso GenerarAlerta (FR-027): una alerta por escaneo con ≥ 1 hallazgo Alto.</summary>
public sealed class GeneradorAlertas(IEgidaDbContext db, IReloj reloj)
{
    /// <summary>Agrega la alerta al contexto (se guarda en la misma transacción que Terminado: outbox, R10).</summary>
    public async Task<Alerta?> CrearSiCorrespondeAsync(Escaneo escaneo, Activo activo, IReadOnlyList<Hallazgo> hallazgos,
        CancellationToken ct)
    {
        var altas = hallazgos.Where(h => h.Severidad == Severidad.Alta).ToList();
        if (altas.Count == 0) return null;

        // El destinatario es siempre el propietario del activo, aunque el escaneo lo lance el Administrador.
        var propietario = await db.Usuarios.FirstAsync(u => u.Id == activo.PropietarioId, ct);
        var posiblesFp = altas.Count(h => h.EsPosibleFalsoPositivo);
        var resumen = $"{altas.Count} hallazgo(s) de severidad Alta en {activo.Nombre}" +
                      (posiblesFp > 0 ? $" ({posiblesFp} marcado(s) por la IA como posible falso positivo)." : ".");
        var alerta = new Alerta(escaneo.Id, propietario.Id, propietario.Correo, altas.Count, posiblesFp, resumen, reloj.Ahora);
        db.Alertas.Add(alerta);
        return alerta;
    }
}

/// <summary>Arma el correo de alerta (FR-028) con el estado de IA vigente al momento del envío.</summary>
public static class ConstructorCorreoAlerta
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-CO");

    public static MensajeCorreo Construir(Alerta alerta, Activo activo, Escaneo escaneo,
        IReadOnlyList<Hallazgo> hallazgos, string urlPublica)
    {
        var altas = hallazgos.Where(h => h.Severidad == Severidad.Alta).OrderBy(h => h.Nombre).ToList();
        int Contar(Severidad s) => hallazgos.Count(h => h.Severidad == s);
        var enlace = $"{urlPublica.TrimEnd('/')}/escaneos/{escaneo.Id}";
        var fecha = (escaneo.FinalizadoEn ?? escaneo.SolicitadoEn).ToOffset(TimeSpan.FromHours(-5))
            .ToString("dd 'de' MMMM 'de' yyyy, HH:mm '(GMT-5)'", Es);
        var asunto = $"ÉGIDA – Alerta: {altas.Count} hallazgos de severidad Alta en {activo.Nombre}";

        static string EstadoIA(Hallazgo h) => h.EstadoIA == EstadoAnalisisIA.Pendiente
            ? "análisis IA pendiente"
            : $"Prioridad IA {h.PrioridadIA}" + (h.EsPosibleFalsoPositivo ? " · posible falso positivo" : "");

        var texto = new StringBuilder()
            .AppendLine(asunto).AppendLine()
            .AppendLine($"Activo: {activo.Nombre} ({activo.Url})")
            .AppendLine($"Fecha del escaneo: {fecha}")
            .AppendLine($"Hallazgos: Alta {Contar(Severidad.Alta)} · Media {Contar(Severidad.Media)} · " +
                        $"Baja {Contar(Severidad.Baja)} · Informativa {Contar(Severidad.Informativa)}")
            .AppendLine().AppendLine("Hallazgos de severidad Alta:");
        foreach (var h in altas)
            texto.AppendLine($"- {h.Nombre} [{h.CategoriaOwasp}] {h.UrlAfectada} — {EstadoIA(h)}");
        texto.AppendLine().AppendLine($"Ver el detalle: {enlace}");

        string E(string s) => WebUtility.HtmlEncode(s);
        var filas = new StringBuilder();
        foreach (var h in altas)
        {
            var ia = h.EstadoIA == EstadoAnalisisIA.Pendiente
                ? "<span style=\"color:#6b7280\">análisis IA pendiente</span>"
                : $"Prioridad IA <b>{h.PrioridadIA}</b>" + (h.EsPosibleFalsoPositivo
                    ? " <span style=\"background:#fef3c7;color:#92400e;padding:1px 6px;border-radius:8px\">posible falso positivo</span>"
                    : "");
            filas.Append($"<tr><td style=\"padding:6px;border-bottom:1px solid #e5e7eb\">{E(h.Nombre)}<br><small style=\"color:#6b7280\">{E(h.UrlAfectada)}</small></td>" +
                         $"<td style=\"padding:6px;border-bottom:1px solid #e5e7eb\">{E(h.CategoriaOwasp)}</td>" +
                         $"<td style=\"padding:6px;border-bottom:1px solid #e5e7eb\">{ia}</td></tr>");
        }

        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:680px;color:#111827">
              <h2 style="color:#b91c1c;margin-bottom:4px">ÉGIDA · Alerta de seguridad</h2>
              <p>El escaneo de <b>{E(activo.Nombre)}</b> ({E(activo.Url)}) terminó el {E(fecha)} con
                 <b>{altas.Count}</b> hallazgo(s) de severidad <b>Alta</b>.</p>
              <p>Alta <b>{Contar(Severidad.Alta)}</b> · Media <b>{Contar(Severidad.Media)}</b> ·
                 Baja <b>{Contar(Severidad.Baja)}</b> · Informativa <b>{Contar(Severidad.Informativa)}</b></p>
              <table style="border-collapse:collapse;width:100%;font-size:14px">
                <tr style="background:#f3f4f6;text-align:left"><th style="padding:6px">Hallazgo</th><th style="padding:6px">OWASP Top 10</th><th style="padding:6px">Análisis IA</th></tr>
                {filas}
              </table>
              <p style="margin-top:20px"><a href="{E(enlace)}" style="background:#1d4ed8;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none">Ver el detalle del escaneo</a></p>
              <p style="color:#6b7280;font-size:12px">Correo generado automáticamente por ÉGIDA (prototipo académico). La severidad proviene del escáner; la Prioridad IA es un dato adicional.</p>
            </div>
            """;

        return new MensajeCorreo(alerta.CorreoDestino, asunto, html, texto.ToString());
    }
}

/// <summary>Caso de uso ListarAlertas (API, FR-030).</summary>
public sealed class ServicioAlertas(IEgidaDbContext db)
{
    public async Task<IReadOnlyList<AlertaDto>> ListarAsync(IUsuarioActual usuario, int? limite, CancellationToken ct = default) =>
        await Consultas.ProyectarAlertasAsync(db, db.AlertasVisibles(usuario), Math.Clamp(limite ?? 20, 1, 100), ct);
}

/// <summary>Caso de uso EnviarAlerta (Worker, FR-029): hasta 3 intentos con backoff.</summary>
public sealed class ServicioEnvioAlertas(
    IEgidaDbContext db, IEnviadorCorreo correo, IPublicadorEventos eventos, IReloj reloj,
    IOptions<OpcionesAlertas> opciones, ILogger<ServicioEnvioAlertas> log)
{
    public async Task<int> EnviarPendientesAsync(CancellationToken ct)
    {
        var ahora = reloj.Ahora;
        var pendientes = await db.Alertas
            .Where(a => a.EstadoEnvio == EstadoEnvio.Pendiente && (a.ProximoIntentoEn == null || a.ProximoIntentoEn <= ahora))
            .OrderBy(a => a.CreadaEn).Take(10).ToListAsync(ct);

        foreach (var alerta in pendientes)
        {
            var escaneo = await db.Escaneos.AsNoTracking().FirstAsync(e => e.Id == alerta.EscaneoId, ct);
            var activo = await db.Activos.AsNoTracking().FirstAsync(a => a.Id == escaneo.ActivoId, ct);
            var hallazgos = await db.Hallazgos.AsNoTracking().Where(h => h.EscaneoId == escaneo.Id).ToListAsync(ct);
            try
            {
                var mensaje = ConstructorCorreoAlerta.Construir(alerta, activo, escaneo, hallazgos, opciones.Value.UrlPublica);
                await correo.EnviarAsync(mensaje, ct);
                alerta.RegistrarEnvioExitoso(reloj.Ahora);
                log.LogInformation("Alerta {Id} enviada a {Correo}", alerta.Id, alerta.CorreoDestino);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                alerta.RegistrarFalloEnvio(ex.Message, reloj.Ahora);
                log.LogWarning(ex, "Falló el envío de la alerta {Id} (intento {N})", alerta.Id, alerta.IntentosEnvio);
            }
            await db.SaveChangesAsync(ct);

            if (alerta.EstadoEnvio != EstadoEnvio.Pendiente)
                await eventos.PublicarAsync(TiposEvento.AlertaEnvioActualizado, activo.PropietarioId,
                    new { alertaId = alerta.Id, estadoEnvio = alerta.EstadoEnvio.ToString() }, ct);
        }
        return pendientes.Count;
    }
}
