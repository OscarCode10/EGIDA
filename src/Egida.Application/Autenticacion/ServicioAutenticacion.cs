using Egida.Application.Abstracciones;
using Egida.Application.Comun;
using Egida.Domain.Auditoria;
using Egida.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Autenticacion;

/// <summary>Caso de uso IniciarSesion (FR-001, FR-005, FR-007).</summary>
public sealed class ServicioAutenticacion(
    IEgidaDbContext db, IHasherContrasenas hasher, IGeneradorTokens tokens, IReloj reloj)
{
    public const string MensajeGenerico = "Correo o contraseña incorrectos";

    public async Task<RespuestaLogin> IniciarSesionAsync(string? correo, string? contrasena, string? ip,
        CancellationToken ct = default)
    {
        var errores = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(correo)) errores["correo"] = ["El correo es obligatorio."];
        if (string.IsNullOrEmpty(contrasena)) errores["contrasena"] = ["La contraseña es obligatoria."];
        if (errores.Count > 0)
            throw new ErrorAplicacion(TipoError.Validacion, "validacion", "La solicitud tiene datos inválidos.", errores);

        var ahora = reloj.Ahora;
        var normalizado = Usuario.NormalizarCorreo(correo!);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == normalizado, ct);

        if (usuario is null)
        {
            Auditar(null, AccionesAuditoria.LoginFallido, ResultadosAuditoria.Fallo, normalizado, ip);
            await db.SaveChangesAsync(ct);
            throw Credenciales();
        }

        if (usuario.EstaBloqueado(ahora))
        {
            Auditar(usuario.Id, AccionesAuditoria.LoginFallido, ResultadosAuditoria.Denegado, normalizado, ip);
            await db.SaveChangesAsync(ct);
            throw Credenciales();
        }

        if (!hasher.Verificar(usuario, contrasena!))
        {
            var seBloqueo = usuario.RegistrarLoginFallido(ahora);
            Auditar(usuario.Id, AccionesAuditoria.LoginFallido, ResultadosAuditoria.Fallo, normalizado, ip);
            if (seBloqueo)
                Auditar(usuario.Id, AccionesAuditoria.CuentaBloqueada, ResultadosAuditoria.Denegado, normalizado, ip);
            await db.SaveChangesAsync(ct);
            throw Credenciales();
        }

        usuario.RegistrarLoginExitoso();
        Auditar(usuario.Id, AccionesAuditoria.LoginExitoso, ResultadosAuditoria.Exito, normalizado, ip);
        await db.SaveChangesAsync(ct);

        var (token, expira) = tokens.Generar(usuario);
        return new RespuestaLogin(token, expira, ADto(usuario));
    }

    public async Task<UsuarioDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct)
                      ?? throw new ErrorAplicacion(TipoError.NoAutorizado, "no_autenticado", "Sesión no válida.");
        return ADto(usuario);
    }

    public static UsuarioDto ADto(Usuario u) => new(u.Id, u.Nombre, u.Correo, u.Rol.ToString());

    private static ErrorAplicacion Credenciales() =>
        new(TipoError.NoAutorizado, "credenciales_invalidas", MensajeGenerico);

    private void Auditar(Guid? usuarioId, string accion, string resultado, string correo, string? ip) =>
        db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuarioId, accion, resultado,
            recurso: "auth/login", correoIntentado: correo, ipOrigen: ip));
}
