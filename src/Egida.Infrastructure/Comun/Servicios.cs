using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Egida.Application.Abstracciones;
using Egida.Application.Escaneos;
using Egida.Domain.Activos;
using Egida.Domain.Usuarios;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using Npgsql;

namespace Egida.Infrastructure.Comun;

public sealed class RelojSistema : IReloj
{
    public DateTimeOffset Ahora => DateTimeOffset.UtcNow;
}

/// <summary>PBKDF2 mediante PasswordHasher de ASP.NET Core Identity (sin el esquema completo de Identity).</summary>
public sealed class HasherContrasenas : IHasherContrasenas
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hash(Usuario usuario, string contrasena) => _hasher.HashPassword(usuario, contrasena);

    public bool Verificar(Usuario usuario, string contrasena) =>
        !string.IsNullOrEmpty(usuario.HashContrasena) &&
        _hasher.VerifyHashedPassword(usuario, usuario.HashContrasena, contrasena) != PasswordVerificationResult.Failed;
}

public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";
    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = "egida";
    public string Audiencia { get; set; } = "egida-web";
    public int MinutosVigencia { get; set; } = 30;

    public SymmetricSecurityKey ClaveFirma()
    {
        if (Encoding.UTF8.GetByteCount(Clave) < 32)
            throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 bytes (variable JWT_CLAVE).");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Clave));
    }
}

/// <summary>JWT HS256, vigencia 30 min, claims sub, email, role (R6).</summary>
public sealed class GeneradorJwt(IOptions<OpcionesJwt> opciones, IReloj reloj) : IGeneradorTokens
{
    public (string Token, DateTimeOffset ExpiraEn) Generar(Usuario usuario)
    {
        var o = opciones.Value;
        var expira = reloj.Ahora.AddMinutes(o.MinutosVigencia);
        var token = new JwtSecurityToken(
            issuer: o.Emisor,
            audience: o.Audiencia,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
                new Claim(JwtRegisteredClaimNames.Name, usuario.Nombre),
                new Claim("role", usuario.Rol.ToString()),
            ],
            notBefore: reloj.Ahora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: new SigningCredentials(o.ClaveFirma(), SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

/// <summary>Lista blanca por origen exacto (esquema + host + puerto), desde configuración (FR-009, principio XII).</summary>
public sealed class ListaBlancaObjetivosConfig(IOptions<OpcionesEscaneo> opciones) : IListaBlancaObjetivos
{
    public IReadOnlyList<ObjetivoPermitido> Objetivos => opciones.Value.Objetivos
        .Where(o => UrlNormalizada.TryCrear(o.Url, out _)).ToList();

    public ObjetivoPermitido? Buscar(UrlNormalizada url) =>
        Objetivos.FirstOrDefault(o => string.Equals(UrlNormalizada.Crear(o.Url).Origen, url.Origen, StringComparison.Ordinal));
}

/// <summary>Emite NOTIFY egida_eventos (R5). La API lo reenvía por SignalR.</summary>
public sealed class PublicadorEventosPostgres(NpgsqlDataSource fuente) : IPublicadorEventos
{
    public const string Canal = "egida_eventos";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task PublicarAsync(string tipo, Guid propietarioId, object datos, CancellationToken ct = default)
    {
        var carga = JsonSerializer.Serialize(new { tipo, propietarioId, datos }, Json);
        await using var cmd = fuente.CreateCommand("SELECT pg_notify(@canal, @carga)");
        cmd.Parameters.AddWithValue("canal", Canal);
        cmd.Parameters.AddWithValue("carga", carga);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}

/// <summary>Cola sobre la tabla escaneos con FOR UPDATE SKIP LOCKED (R4).</summary>
public sealed class ColaEscaneosPostgres(NpgsqlDataSource fuente) : IColaEscaneos
{
    public async Task<Guid?> TomarSiguienteAsync(CancellationToken ct)
    {
        await using var cmd = fuente.CreateCommand("""
            UPDATE escaneos SET estado = 'EnCurso', iniciado_en = now()
            WHERE id = (SELECT id FROM escaneos WHERE estado = 'Pendiente'
                        ORDER BY solicitado_en LIMIT 1 FOR UPDATE SKIP LOCKED)
            RETURNING id
            """);
        return await cmd.ExecuteScalarAsync(ct) as Guid?;
    }

    public async Task<IReadOnlyList<Guid>> MarcarInterrumpidosAsync(CancellationToken ct)
    {
        await using var cmd = fuente.CreateCommand("""
            UPDATE escaneos SET estado = 'Fallido', motivo_fallo = 'Interrumpido', finalizado_en = now()
            WHERE estado = 'EnCurso' RETURNING id
            """);
        var ids = new List<Guid>();
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        while (await lector.ReadAsync(ct)) ids.Add(lector.GetGuid(0));
        return ids;
    }
}

public sealed class OpcionesCorreo
{
    public const string Seccion = "Correo";
    public string Host { get; set; } = "localhost";
    public int Puerto { get; set; } = 1025;
    /// <summary>Ninguna (Mailpit), StartTls (Gmail, puerto 587) o SslTls (puerto 465).</summary>
    public string Seguridad { get; set; } = "Ninguna";
    /// <summary>Usuario SMTP; si está vacío no se autentica (Mailpit).</summary>
    public string? Usuario { get; set; }
    /// <summary>Contraseña SMTP (en Gmail, una contraseña de aplicación). Solo desde variables de entorno.</summary>
    public string? Contrasena { get; set; }
    /// <summary>Dirección del remitente; si está vacía se usa <see cref="Usuario"/> (Gmail lo exige).</summary>
    public string? Remitente { get; set; }
    public string NombreRemitente { get; set; } = "ÉGIDA Alertas";

    public SecureSocketOptions OpcionSeguridad => Seguridad.Trim().ToLowerInvariant() switch
    {
        "starttls" => SecureSocketOptions.StartTls,
        "ssltls" or "ssl" => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.None,
    };

    public string DireccionRemitente =>
        !string.IsNullOrWhiteSpace(Remitente) ? Remitente
        : !string.IsNullOrWhiteSpace(Usuario) ? Usuario
        : "alertas@egida.test";
}

/// <summary>SMTP estándar con MailKit (principio IX): Mailpit en pruebas o un SMTP real (p. ej. Gmail).</summary>
public sealed class EnviadorCorreoMailKit(IOptions<OpcionesCorreo> opciones) : IEnviadorCorreo
{
    public async Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        var o = opciones.Value;
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(o.NombreRemitente, o.DireccionRemitente));
        mime.To.Add(MailboxAddress.Parse(mensaje.Para));
        mime.Subject = mensaje.Asunto;
        mime.Body = new BodyBuilder { HtmlBody = mensaje.CuerpoHtml, TextBody = mensaje.CuerpoTexto }.ToMessageBody();

        using var smtp = new SmtpClient { Timeout = 15000 };
        await smtp.ConnectAsync(o.Host, o.Puerto, o.OpcionSeguridad, ct);
        if (!string.IsNullOrWhiteSpace(o.Usuario))
            await smtp.AuthenticateAsync(o.Usuario, o.Contrasena ?? string.Empty, ct);
        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
