using Egida.Application.Abstracciones;
using Egida.Application.Activos;
using Egida.Application.Alertas;
using Egida.Application.Autenticacion;
using Egida.Application.Dashboard;
using Egida.Application.Escaneos;

namespace Egida.Api.Endpoints;

/// <summary>Minimal APIs según contracts/api-rest.md. Roles declarados explícitamente en cada grupo.</summary>
public static class EndpointsEgida
{
    private const string Ambos = "Cliente,Administrador";

    public sealed record SolicitudLogin(string? Correo, string? Contrasena);

    public sealed record SolicitudActivo(string? Nombre, string? Url);

    public static void MapEndpointsAutenticacion(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Autenticación");

        g.MapPost("/login", async (SolicitudLogin? s, ServicioAutenticacion auth, HttpContext ctx, CancellationToken ct) =>
                Results.Ok(await auth.IniciarSesionAsync(s?.Correo, s?.Contrasena,
                    ctx.Connection.RemoteIpAddress?.ToString(), ct)))
            .AllowAnonymous()
            .RequireRateLimiting("login");

        g.MapGet("/yo", async (IUsuarioActual u, ServicioAutenticacion auth, CancellationToken ct) =>
                Results.Ok(await auth.ObtenerAsync(u.Id, ct)))
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador"));
    }

    public static void MapEndpointsActivos(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/objetivos-permitidos", (ServicioActivos s) => Results.Ok(s.ObjetivosPermitidos()))
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador")).WithTags("Activos");

        var g = app.MapGroup("/api/activos").WithTags("Activos")
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador"));

        g.MapGet("/", async (ServicioActivos s, CancellationToken ct) => Results.Ok(await s.ListarAsync(ct)));

        g.MapPost("/", async (SolicitudActivo? body, ServicioActivos s, CancellationToken ct) =>
        {
            var activo = await s.RegistrarAsync(body?.Nombre, body?.Url, ct);
            return Results.Created($"/api/activos/{activo.Id}", activo);
        }).RequireAuthorization(p => p.RequireRole("Cliente"));

        g.MapGet("/{id:guid}", async (Guid id, ServicioActivos s, CancellationToken ct) =>
            Results.Ok(await s.ObtenerAsync(id, ct)));

        g.MapPost("/{id:guid}/escaneos", async (Guid id, ServicioEscaneos s, CancellationToken ct) =>
        {
            var escaneo = await s.SolicitarAsync(id, ct);
            return Results.Accepted($"/api/escaneos/{escaneo.Id}", escaneo);
        });
    }

    public static void MapEndpointsEscaneos(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/escaneos").WithTags("Escaneos")
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador"));

        g.MapGet("/", async (Guid? activoId, int? limite, ServicioEscaneos s, CancellationToken ct) =>
            Results.Ok(await s.ListarAsync(activoId, limite, ct)));

        g.MapGet("/{id:guid}", async (Guid id, ServicioEscaneos s, CancellationToken ct) =>
            Results.Ok(await s.ObtenerAsync(id, ct)));

        g.MapGet("/{id:guid}/hallazgos", async (Guid id, string? severidad, string? orden, ServicioEscaneos s,
                CancellationToken ct) => Results.Ok(await s.HallazgosAsync(id, severidad, orden, ct)));
    }

    public static void MapEndpointsAlertas(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/alertas", async (int? limite, ServicioAlertas s, IUsuarioActual u, CancellationToken ct) =>
                Results.Ok(await s.ListarAsync(u, limite, ct)))
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador")).WithTags("Alertas");

    public static void MapEndpointsDashboard(this IEndpointRouteBuilder app) =>
        app.MapGet("/api/dashboard", async (ServicioDashboard s, CancellationToken ct) => Results.Ok(await s.ObtenerAsync(ct)))
            .RequireAuthorization(p => p.RequireRole("Cliente", "Administrador")).WithTags("Dashboard");

    internal static string RolesAmbos => Ambos;
}
