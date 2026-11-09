using Egida.Application.Abstracciones;
using Egida.Application.Comun;
using Egida.Domain.Activos;
using Egida.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Activos;

/// <summary>Casos de uso RegistrarActivo, ListarActivos y ObtenerActivo (FR-008 a FR-011).</summary>
public sealed class ServicioActivos(
    IEgidaDbContext db, IUsuarioActual usuario, IListaBlancaObjetivos listaBlanca, IReloj reloj)
{
    public const string MensajeNoAutorizado = "Solo se permiten objetivos del entorno de pruebas autorizado.";

    public IReadOnlyList<ObjetivoDto> ObjetivosPermitidos() =>
        listaBlanca.Objetivos.Select(o => new ObjetivoDto(o.Nombre, UrlNormalizada.Crear(o.Url).Valor)).ToList();

    public async Task<IReadOnlyList<ActivoDto>> ListarAsync(CancellationToken ct = default)
    {
        var filas = await (
                from a in db.ActivosVisibles(usuario)
                join p in db.Usuarios on a.PropietarioId equals p.Id
                orderby a.CreadoEn descending
                select new { a, p })
            .AsNoTracking().ToListAsync(ct);

        var ids = filas.Select(f => f.a.Id).ToList();
        var ultimos = await db.Escaneos.AsNoTracking()
            .Where(e => ids.Contains(e.ActivoId))
            .GroupBy(e => e.ActivoId)
            .Select(g => g.OrderByDescending(e => e.SolicitadoEn).First())
            .ToListAsync(ct);

        return filas.Select(f =>
        {
            var u = ultimos.FirstOrDefault(e => e.ActivoId == f.a.Id);
            return new ActivoDto(f.a.Id, f.a.Nombre, f.a.Url, new PersonaDto(f.p.Id, f.p.Nombre), f.a.CreadoEn,
                u is null ? null : new UltimoEscaneoDto(u.Id, u.Estado.ToString(), u.FinalizadoEn));
        }).ToList();
    }

    public async Task<ActivoDto> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        await ObtenerVisibleAsync(id, ct);
        return (await ListarAsync(ct)).First(a => a.Id == id);
    }

    /// <summary>Devuelve el activo si el usuario puede verlo; si no, 404 y auditoría AccesoDenegado.</summary>
    public async Task<Activo> ObtenerVisibleAsync(Guid id, CancellationToken ct = default)
    {
        var activo = await db.ActivosVisibles(usuario).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (activo is not null) return activo;

        if (await db.Activos.AnyAsync(a => a.Id == id, ct))
        {
            db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuario.Id, AccionesAuditoria.AccesoDenegado,
                ResultadosAuditoria.Denegado, recurso: $"activos/{id}", ipOrigen: usuario.IpOrigen));
            await db.SaveChangesAsync(ct);
        }
        throw ErrorAplicacion.NoEncontrado("el activo");
    }

    public async Task<ActivoDto> RegistrarAsync(string? nombre, string? url, CancellationToken ct = default)
    {
        if (usuario.EsAdministrador)
            throw new ErrorAplicacion(TipoError.Prohibido, "prohibido", "El Administrador no registra activos.");

        var errores = new Dictionary<string, string[]>();
        var nombreLimpio = nombre?.Trim() ?? string.Empty;
        if (nombreLimpio.Length is < 1 or > 100) errores["nombre"] = ["El nombre es obligatorio (1 a 100 caracteres)."];
        if (!UrlNormalizada.TryCrear(url, out var normalizada))
            errores["url"] = ["Ingrese una URL http o https válida, por ejemplo http://juice-shop:3000."];
        if (errores.Count > 0)
            throw new ErrorAplicacion(TipoError.Validacion, "validacion", "La solicitud tiene datos inválidos.", errores);

        if (listaBlanca.Buscar(normalizada!) is null)
            throw new ErrorAplicacion(TipoError.Validacion, "objetivo_no_autorizado", MensajeNoAutorizado);

        if (await db.Activos.AnyAsync(a => a.PropietarioId == usuario.Id && a.Url == normalizada!.Valor, ct))
            throw new ErrorAplicacion(TipoError.Conflicto, "activo_duplicado", "Ya registraste un activo con esa URL.");

        var activo = new Activo(nombreLimpio, normalizada!, usuario.Id, reloj.Ahora);
        db.Activos.Add(activo);
        db.RegistrosAuditoria.Add(new RegistroAuditoria(reloj.Ahora, usuario.Id, AccionesAuditoria.ActivoRegistrado,
            ResultadosAuditoria.Exito, recurso: $"activos/{activo.Id}", ipOrigen: usuario.IpOrigen));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new ErrorAplicacion(TipoError.Conflicto, "activo_duplicado", "Ya registraste un activo con esa URL.");
        }

        var propietario = await db.Usuarios.AsNoTracking().FirstAsync(u => u.Id == usuario.Id, ct);
        return new ActivoDto(activo.Id, activo.Nombre, activo.Url, new PersonaDto(propietario.Id, propietario.Nombre),
            activo.CreadoEn, null);
    }
}
