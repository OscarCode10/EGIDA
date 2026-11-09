using Egida.Application.Abstracciones;
using Egida.Application.Escaneos;
using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Auditoria;
using Egida.Domain.Escaneos;
using Egida.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace Egida.Application.Tests;

/// <summary>Base en memoria para probar casos de uso sin PostgreSQL.</summary>
public sealed class DbPrueba(DbContextOptions<DbPrueba> o) : DbContext(o), IEgidaDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Escaneo> Escaneos => Set<Escaneo>();
    public DbSet<Hallazgo> Hallazgos => Set<Hallazgo>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Hallazgo>().Ignore(h => h.EsPosibleFalsoPositivo);
        mb.Entity<Escaneo>().Ignore(e => e.EsFinal);
    }

    public static DbPrueba Nueva() =>
        new(new DbContextOptionsBuilder<DbPrueba>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}

public sealed class RelojFijo : IReloj
{
    public DateTimeOffset Ahora { get; set; } = new(2026, 11, 8, 15, 0, 0, TimeSpan.Zero);
}

public sealed class UsuarioPrueba(Guid id, Rol rol) : IUsuarioActual
{
    public Guid Id { get; } = id;
    public Rol Rol { get; } = rol;
    public string? IpOrigen => "127.0.0.1";
}

public sealed class HasherFalso : IHasherContrasenas
{
    public string Hash(Usuario usuario, string contrasena) => "h:" + contrasena;
    public bool Verificar(Usuario usuario, string contrasena) => usuario.HashContrasena == "h:" + contrasena;
}

public sealed class TokensFalsos : IGeneradorTokens
{
    public (string Token, DateTimeOffset ExpiraEn) Generar(Usuario usuario) => ("token-" + usuario.Id, DateTimeOffset.UtcNow.AddMinutes(30));
}

public sealed class ListaBlancaFija : IListaBlancaObjetivos
{
    public List<ObjetivoPermitido> Lista { get; } = [new() { Url = "http://juice-shop:3000", Nombre = "Juice Shop" }];
    public IReadOnlyList<ObjetivoPermitido> Objetivos => Lista;
    public ObjetivoPermitido? Buscar(UrlNormalizada url) =>
        Lista.FirstOrDefault(o => UrlNormalizada.Crear(o.Url).Origen == url.Origen);
}

public sealed class EventosEnMemoria : IPublicadorEventos
{
    public List<(string Tipo, Guid Propietario, object Datos)> Publicados { get; } = [];

    public Task PublicarAsync(string tipo, Guid propietarioId, object datos, CancellationToken ct = default)
    {
        Publicados.Add((tipo, propietarioId, datos));
        return Task.CompletedTask;
    }
}

public sealed class EscanerFalso : IEscaner
{
    public Func<CancellationToken, Task<IReadOnlyList<DatosHallazgo>>> Comportamiento { get; set; } =
        _ => Task.FromResult<IReadOnlyList<DatosHallazgo>>([]);
    public int Llamadas { get; private set; }

    public Task<IReadOnlyList<DatosHallazgo>> EscanearAsync(ObjetivoPermitido objetivo, string urlActivo, CancellationToken ct)
    {
        Llamadas++;
        return Comportamiento(ct);
    }
}

public sealed class IAFalsa : IAnalizadorIA
{
    public bool Disponible { get; set; } = true;
    public decimal Probabilidad { get; set; } = 0.1m;
    public int Llamadas { get; private set; }

    public Task<RespuestaIA?> AnalizarAsync(Guid escaneoId, IReadOnlyList<Hallazgo> hallazgos, CancellationToken ct)
    {
        Llamadas++;
        if (!Disponible) return Task.FromResult<RespuestaIA?>(null);
        return Task.FromResult<RespuestaIA?>(new RespuestaIA("rf-prueba",
            hallazgos.Select(h => new ResultadoIA(h.Id, h.Severidad == Severidad.Alta ? PrioridadIA.Alta : PrioridadIA.Baja,
                Probabilidad, "Justificación de prueba.")).ToList()));
    }

    public Task<bool> DisponibleAsync(CancellationToken ct) => Task.FromResult(Disponible);
}

public sealed class CorreoFalso : IEnviadorCorreo
{
    public List<MensajeCorreo> Enviados { get; } = [];
    public bool Falla { get; set; }

    public Task EnviarAsync(MensajeCorreo mensaje, CancellationToken ct)
    {
        if (Falla) throw new InvalidOperationException("SMTP caído");
        Enviados.Add(mensaje);
        return Task.CompletedTask;
    }
}
