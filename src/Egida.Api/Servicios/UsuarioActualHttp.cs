using Egida.Application.Abstracciones;
using Egida.Application.Comun;
using Egida.Domain.Usuarios;

namespace Egida.Api.Servicios;

/// <summary>Usuario autenticado a partir de los claims del JWT.</summary>
public sealed class UsuarioActualHttp(IHttpContextAccessor accesor) : IUsuarioActual
{
    private HttpContext Contexto => accesor.HttpContext
                                    ?? throw new InvalidOperationException("No hay solicitud HTTP en curso.");

    public Guid Id => Guid.TryParse(Contexto.User.FindFirst("sub")?.Value, out var id)
        ? id
        : throw new ErrorAplicacion(TipoError.NoAutorizado, "no_autenticado", "Sesión no válida.");

    public Rol Rol => Enum.TryParse<Rol>(Contexto.User.FindFirst("role")?.Value, out var rol)
        ? rol
        : throw new ErrorAplicacion(TipoError.NoAutorizado, "no_autenticado", "Sesión no válida.");

    public string? IpOrigen => Contexto.Connection.RemoteIpAddress?.ToString();
}
