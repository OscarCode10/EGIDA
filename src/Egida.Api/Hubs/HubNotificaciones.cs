using System.Text.Json;
using Egida.Infrastructure.Comun;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Npgsql;

namespace Egida.Api.Hubs;

/// <summary>
/// Hub de notificaciones (contracts/signalr-notificaciones.md). No expone métodos cliente → servidor:
/// el servidor asigna los grupos usuario-{id} y administradores al conectar.
/// </summary>
[Authorize(Roles = "Cliente,Administrador")]
public sealed class HubNotificaciones : Hub
{
    public const string GrupoAdministradores = "administradores";

    public static string GrupoUsuario(string id) => $"usuario-{id}";

    public override async Task OnConnectedAsync()
    {
        var id = Context.User?.FindFirst("sub")?.Value;
        if (id is null)
        {
            Context.Abort();
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoUsuario(id));
        if (Context.User!.IsInRole("Administrador"))
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoAdministradores);
        await base.OnConnectedAsync();
    }
}

/// <summary>
/// Escucha LISTEN egida_eventos (lo emite el Worker o la API) y reenvía por SignalR al propietario
/// y a los administradores (R5). Si la conexión se cae, reintenta con backoff.
/// </summary>
public sealed class ReenviadorEventos(NpgsqlDataSource fuente, IHubContext<HubNotificaciones> hub,
    ILogger<ReenviadorEventos> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var espera = TimeSpan.FromSeconds(1);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var conexion = await fuente.OpenConnectionAsync(ct);
                conexion.Notification += (_, e) => _ = ReenviarAsync(e.Payload);
                await using (var cmd = new NpgsqlCommand($"LISTEN {PublicadorEventosPostgres.Canal}", conexion))
                    await cmd.ExecuteNonQueryAsync(ct);
                log.LogInformation("Escuchando eventos en {Canal}", PublicadorEventosPostgres.Canal);
                espera = TimeSpan.FromSeconds(1);
                while (!ct.IsCancellationRequested) await conexion.WaitAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                log.LogWarning("Conexión LISTEN perdida ({Error}); reintento en {Espera}", ex.Message, espera);
                await Task.Delay(espera, ct);
                espera = TimeSpan.FromSeconds(Math.Min(espera.TotalSeconds * 2, 30));
            }
        }
    }

    private async Task ReenviarAsync(string carga)
    {
        try
        {
            using var doc = JsonDocument.Parse(carga);
            var tipo = doc.RootElement.GetProperty("tipo").GetString()!;
            var propietario = doc.RootElement.GetProperty("propietarioId").GetString()!;
            var datos = doc.RootElement.GetProperty("datos").Clone();
            await hub.Clients.Groups(HubNotificaciones.GrupoUsuario(propietario), HubNotificaciones.GrupoAdministradores)
                .SendAsync(tipo, datos);
        }
        catch (Exception ex)
        {
            log.LogWarning("Evento inválido descartado: {Error}", ex.Message);
        }
    }
}
