using System.Security.Claims;
using System.Threading.RateLimiting;
using Egida.Api.Endpoints;
using Egida.Api.Hubs;
using Egida.Api.Servicios;
using Egida.Application;
using Egida.Application.Abstracciones;
using Egida.Infrastructure;
using Egida.Infrastructure.Comun;
using Egida.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AgregarInfraestructura(builder.Configuration);
builder.Services.AgregarAplicacion();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddHostedService<ReenviadorEventos>();

// R6: JWT Bearer HS256. SignalR recibe el token por query solo en /hubs/*.
var jwt = builder.Configuration.GetSection(OpcionesJwt.Seccion).Get<OpcionesJwt>() ?? new OpcionesJwt();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = jwt.ClaveFirma(),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "sub",
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });

// FR-002: denegar por defecto. Cada endpoint declara sus roles o AllowAnonymous.
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment()) app.MapOpenApi().AllowAnonymous();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapGet("/salud", () => Results.Ok(new { estado = "ok" })).AllowAnonymous();
app.MapEndpointsAutenticacion();
app.MapEndpointsActivos();
app.MapEndpointsEscaneos();
app.MapEndpointsAlertas();
app.MapEndpointsDashboard();
app.MapHub<HubNotificaciones>("/hubs/notificaciones").RequireAuthorization();

// Migraciones y usuarios semilla al arrancar (FR-034, R7).
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EgidaDbContext>();
    for (var intento = 1; ; intento++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (intento < 10)
        {
            app.Logger.LogWarning("Base de datos no disponible ({Error}); reintento {N}", ex.Message, intento);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
    await scope.ServiceProvider.GetRequiredService<SembradorUsuarios>().SembrarAsync();
}

app.Run();

public partial class Program;
