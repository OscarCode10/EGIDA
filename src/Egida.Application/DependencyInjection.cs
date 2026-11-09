using Egida.Application.Activos;
using Egida.Application.Alertas;
using Egida.Application.AnalisisIA;
using Egida.Application.Autenticacion;
using Egida.Application.Dashboard;
using Egida.Application.Escaneos;
using Microsoft.Extensions.DependencyInjection;

namespace Egida.Application;

public static class DependencyInjection
{
    /// <summary>Casos de uso que atiende la API (respuesta inmediata, principio VI).</summary>
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddScoped<ServicioAutenticacion>();
        servicios.AddScoped<ServicioActivos>();
        servicios.AddScoped<ServicioEscaneos>();
        servicios.AddScoped<ServicioAlertas>();
        servicios.AddScoped<ServicioDashboard>();
        return servicios;
    }

    /// <summary>Casos de uso diferidos que corren en el Worker: escaneo, IA y correo.</summary>
    public static IServiceCollection AgregarCasosUsoWorker(this IServiceCollection servicios)
    {
        servicios.AddScoped<EjecutorEscaneos>();
        servicios.AddScoped<ServicioAnalisisIA>();
        servicios.AddScoped<GeneradorAlertas>();
        servicios.AddScoped<ServicioEnvioAlertas>();
        return servicios;
    }
}
