using Egida.Domain.Comun;

namespace Egida.Domain.Activos;

/// <summary>
/// URL http/https absoluta normalizada: esquema y host en minúsculas, sin puerto por defecto,
/// sin fragmento y sin barra final (salvo la raíz).
/// </summary>
public sealed record UrlNormalizada
{
    public string Valor { get; }

    /// <summary>esquema://host[:puerto]. Se compara con la lista blanca (FR-009).</summary>
    public string Origen { get; }

    private UrlNormalizada(string valor, string origen)
    {
        Valor = valor;
        Origen = origen;
    }

    public static bool TryCrear(string? texto, out UrlNormalizada? url)
    {
        url = null;
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > 2048) return false;
        if (!Uri.TryCreate(texto.Trim(), UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;
        if (string.IsNullOrEmpty(uri.Host)) return false;

        var host = uri.Host.ToLowerInvariant();
        var origen = uri.IsDefaultPort ? $"{uri.Scheme}://{host}" : $"{uri.Scheme}://{host}:{uri.Port}";
        var ruta = uri.AbsolutePath;
        ruta = ruta.Length > 1 ? ruta.TrimEnd('/') : string.Empty;

        url = new UrlNormalizada(origen + ruta + uri.Query, origen);
        return true;
    }

    public static UrlNormalizada Crear(string? texto) =>
        TryCrear(texto, out var url)
            ? url!
            : throw new ExcepcionDominio("validacion", "La URL debe ser una dirección http o https válida.");

    public override string ToString() => Valor;
}

public class Activo
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public Guid PropietarioId { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    private Activo() { }

    public Activo(string nombre, UrlNormalizada url, Guid propietarioId, DateTimeOffset ahora)
    {
        var limpio = nombre?.Trim() ?? string.Empty;
        if (limpio.Length is < 1 or > 100)
            throw new ExcepcionDominio("validacion", "El nombre es obligatorio (1 a 100 caracteres).");

        Id = Guid.CreateVersion7();
        Nombre = limpio;
        Url = url.Valor;
        PropietarioId = propietarioId;
        CreadoEn = ahora;
    }
}
