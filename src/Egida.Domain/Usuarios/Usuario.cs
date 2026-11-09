using Egida.Domain.Comun;

namespace Egida.Domain.Usuarios;

public enum Rol
{
    Administrador,
    Cliente,
}

public class Usuario
{
    public const int MaxFallos = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string Correo { get; private set; } = null!;
    public string HashContrasena { get; private set; } = string.Empty;
    public Rol Rol { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; }

    private Usuario() { }

    public Usuario(string nombre, string correo, Rol rol, DateTimeOffset ahora)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            throw new ExcepcionDominio("validacion", "El nombre es obligatorio (máx. 100 caracteres).");
        ValidarCorreo(correo);

        Id = Guid.CreateVersion7();
        Nombre = nombre.Trim();
        Correo = NormalizarCorreo(correo);
        Rol = rol;
        CreadoEn = ahora;
    }

    public void CambiarCorreo(string correo)
    {
        ValidarCorreo(correo);
        Correo = NormalizarCorreo(correo);
    }

    private static void ValidarCorreo(string correo)
    {
        if (string.IsNullOrWhiteSpace(correo) || correo.Length > 254 || !correo.Contains('@'))
            throw new ExcepcionDominio("validacion", "El correo no es válido.");
    }

    public static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    public void EstablecerHash(string hash) => HashContrasena = hash;

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    /// <summary>Registra un fallo. Devuelve true si este fallo provocó el bloqueo (FR-005).</summary>
    public bool RegistrarLoginFallido(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos < MaxFallos) return false;

        BloqueadoHasta = ahora + DuracionBloqueo;
        IntentosFallidos = 0;
        return true;
    }

    public void RegistrarLoginExitoso()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }
}
