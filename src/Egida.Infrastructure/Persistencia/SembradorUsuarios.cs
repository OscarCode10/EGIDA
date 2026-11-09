using Egida.Application.Abstracciones;
using Egida.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Egida.Infrastructure.Persistencia;

/// <summary>
/// Crea los dos usuarios semilla si no existen (FR-034, R7). Correos y contraseñas vienen de la
/// configuración (Semilla:AdminCorreo, Semilla:ClienteCorreo, ...); por defecto, correos del dominio
/// reservado .test (RFC 2606). Si un usuario semilla ya existe con el correo por defecto y se configura
/// otro, se le actualiza el correo para que las alertas lleguen a una bandeja real.
/// </summary>
public sealed class SembradorUsuarios(EgidaDbContext db, IHasherContrasenas hasher, IReloj reloj,
    IConfiguration config, ILogger<SembradorUsuarios> log)
{
    public const string CorreoAdmin = "admin@egida.test";
    public const string CorreoCliente = "cliente@egida.test";

    public async Task SembrarAsync(CancellationToken ct = default)
    {
        await CrearSiFaltaAsync("Administrador Demo", CorreoAdmin, config["Semilla:AdminCorreo"], Rol.Administrador, "Semilla:AdminContrasena", ct);
        await CrearSiFaltaAsync("Cliente Demo", CorreoCliente, config["Semilla:ClienteCorreo"], Rol.Cliente, "Semilla:ClienteContrasena", ct);
    }

    private async Task CrearSiFaltaAsync(string nombre, string correoPorDefecto, string? correoConfigurado, Rol rol,
        string claveConfig, CancellationToken ct)
    {
        var correo = string.IsNullOrWhiteSpace(correoConfigurado)
            ? correoPorDefecto
            : Usuario.NormalizarCorreo(correoConfigurado);
        if (await db.Usuarios.AnyAsync(u => u.Correo == correo, ct)) return;

        if (correo != correoPorDefecto)
        {
            var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correoPorDefecto && u.Rol == rol, ct);
            if (existente is not null)
            {
                existente.CambiarCorreo(correo);
                await db.SaveChangesAsync(ct);
                log.LogInformation("Usuario semilla {Rol}: correo actualizado a {Correo}", rol, correo);
                return;
            }
        }

        var contrasena = config[claveConfig];
        if (string.IsNullOrWhiteSpace(contrasena))
            throw new InvalidOperationException($"Falta la configuración {claveConfig} para crear el usuario semilla {correo}.");

        var usuario = new Usuario(nombre, correo, rol, reloj.Ahora);
        usuario.EstablecerHash(hasher.Hash(usuario, contrasena));
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Usuario semilla creado: {Correo} ({Rol})", correo, rol);
    }
}
