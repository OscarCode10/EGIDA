using Egida.Application.Comun;
using Egida.Domain.Comun;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Egida.Api.Servicios;

/// <summary>Traduce errores esperados a ProblemDetails (RFC 9457) con title en español y extensión "codigo".</summary>
public sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (estado, codigo, titulo, errores) = ex switch
        {
            ErrorAplicacion e => (e.Tipo switch
            {
                TipoError.Validacion => StatusCodes.Status400BadRequest,
                TipoError.NoAutorizado => StatusCodes.Status401Unauthorized,
                TipoError.Prohibido => StatusCodes.Status403Forbidden,
                TipoError.NoEncontrado => StatusCodes.Status404NotFound,
                TipoError.Conflicto => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest,
            }, e.Codigo, e.Message, e.Errores),
            ExcepcionDominio d => (StatusCodes.Status400BadRequest, d.Codigo, d.Message, null),
            BadHttpRequestException b => (StatusCodes.Status400BadRequest, "validacion", "La solicitud no es válida.", null),
            _ => (StatusCodes.Status500InternalServerError, "error_interno", "Ocurrió un error inesperado.", (IDictionary<string, string[]>?)null),
        };
        if (estado == StatusCodes.Status500InternalServerError) log.LogError(ex, "Error no controlado");

        var detalle = errores is null ? new ProblemDetails() : new ValidationProblemDetails(errores);
        detalle.Status = estado;
        detalle.Title = titulo;
        detalle.Extensions["codigo"] = codigo;
        ctx.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext { HttpContext = ctx, ProblemDetails = detalle, Exception = ex });
    }
}
