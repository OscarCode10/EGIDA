using Egida.Application.Abstracciones;
using Egida.Application.Alertas;
using Egida.Application.Escaneos;
using Egida.Infrastructure.Comun;
using Egida.Infrastructure.IA;
using Egida.Infrastructure.Persistencia;
using Egida.Infrastructure.Zap;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Npgsql;

namespace Egida.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Servicios comunes a la API y al Worker.</summary>
    public static IServiceCollection AgregarInfraestructura(this IServiceCollection s, IConfiguration config)
    {
        var cadena = config.GetConnectionString("Egida")
                     ?? throw new InvalidOperationException("Falta ConnectionStrings:Egida.");

        s.AddSingleton(_ => NpgsqlDataSource.Create(cadena));
        s.AddDbContext<EgidaDbContext>(o => o.UseNpgsql(cadena).UseSnakeCaseNamingConvention());
        s.AddScoped<IEgidaDbContext>(sp => sp.GetRequiredService<EgidaDbContext>());

        s.Configure<OpcionesEscaneo>(config.GetSection(OpcionesEscaneo.Seccion));
        s.Configure<OpcionesJwt>(config.GetSection(OpcionesJwt.Seccion));
        s.Configure<OpcionesAlertas>(config.GetSection("Egida"));

        s.AddSingleton<IReloj, RelojSistema>();
        s.AddSingleton<IHasherContrasenas, HasherContrasenas>();
        s.AddSingleton<IGeneradorTokens, GeneradorJwt>();
        s.AddSingleton<IListaBlancaObjetivos, ListaBlancaObjetivosConfig>();
        s.AddSingleton<IPublicadorEventos, PublicadorEventosPostgres>();
        s.AddSingleton<IColaEscaneos, ColaEscaneosPostgres>();
        s.AddScoped<SembradorUsuarios>();
        return s;
    }

    /// <summary>Clientes de servicios externos que solo usa el Worker: ZAP, IA y SMTP.</summary>
    public static IServiceCollection AgregarClientesExternos(this IServiceCollection s, IConfiguration config)
    {
        s.Configure<OpcionesZap>(config.GetSection(OpcionesZap.Seccion));
        s.Configure<OpcionesIA>(config.GetSection(OpcionesIA.Seccion));
        s.Configure<OpcionesCorreo>(config.GetSection(OpcionesCorreo.Seccion));

        s.AddHttpClient<IEscaner, ClienteZap>(c =>
        {
            c.BaseAddress = new Uri(config[$"{OpcionesZap.Seccion}:Url"] ?? "http://zap:8080");
            c.Timeout = TimeSpan.FromMinutes(2);
        });

        // R9: timeout 10 s, 2 reintentos con backoff y circuit breaker.
        s.AddHttpClient<IAnalizadorIA, ClienteIA>(c =>
            {
                c.BaseAddress = new Uri((config[$"{OpcionesIA.Seccion}:Url"] ?? "http://egida-ia:8000").TrimEnd('/') + "/");
            })
            .AddStandardResilienceHandler(o =>
            {
                o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(40);
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.Delay = TimeSpan.FromSeconds(1);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            });

        s.AddSingleton<IEnviadorCorreo, EnviadorCorreoMailKit>();
        return s;
    }
}
