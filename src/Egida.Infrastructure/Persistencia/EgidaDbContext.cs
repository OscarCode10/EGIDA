using Egida.Application.Abstracciones;
using Egida.Domain.Activos;
using Egida.Domain.Alertas;
using Egida.Domain.Auditoria;
using Egida.Domain.Escaneos;
using Egida.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Egida.Infrastructure.Persistencia;

/// <summary>Base "egida". Tablas y columnas en snake_case (data-model.md).</summary>
public sealed class EgidaDbContext(DbContextOptions<EgidaDbContext> opciones) : DbContext(opciones), IEgidaDbContext
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Activo> Activos => Set<Activo>();
    public DbSet<Escaneo> Escaneos => Set<Escaneo>();
    public DbSet<Hallazgo> Hallazgos => Set<Hallazgo>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<RegistroAuditoria> RegistrosAuditoria => Set<RegistroAuditoria>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Correo).HasMaxLength(254).IsRequired();
            e.HasIndex(x => x.Correo).IsUnique();
            e.Property(x => x.HashContrasena).IsRequired();
            e.Property(x => x.Rol).HasConversion<string>().HasMaxLength(20);
        });

        mb.Entity<Activo>(e =>
        {
            e.ToTable("activos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
            e.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.PropietarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PropietarioId, x.Url }).IsUnique(); // FR-010
        });

        mb.Entity<Escaneo>(e =>
        {
            e.ToTable("escaneos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MotivoFallo).HasMaxLength(500);
            e.HasOne<Activo>().WithMany().HasForeignKey(x => x.ActivoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.SolicitadoPorId).OnDelete(DeleteBehavior.Restrict);
            e.Ignore(x => x.EsFinal);
            // FR-015: un solo escaneo activo por activo, incluso con solicitudes simultáneas.
            e.HasIndex(x => x.ActivoId).IsUnique()
                .HasFilter("estado IN ('Pendiente','EnCurso')")
                .HasDatabaseName("ux_escaneos_activo_en_marcha");
            e.HasIndex(x => new { x.Estado, x.SolicitadoEn });
        });

        mb.Entity<Hallazgo>(e =>
        {
            e.ToTable("hallazgos");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).HasMaxLength(300).IsRequired();
            e.Property(x => x.Descripcion).IsRequired();
            e.Property(x => x.Severidad).IsRequired(); // int: el orden numérico permite ordenar
            e.Property(x => x.CategoriaOwasp).HasMaxLength(60).IsRequired();
            e.Property(x => x.OrigenCategoria).HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.UrlAfectada).HasMaxLength(2048).IsRequired();
            e.Property(x => x.Recomendacion).IsRequired();
            e.Property(x => x.MetodoHttp).HasMaxLength(10);
            e.Property(x => x.Parametro).HasMaxLength(300);
            e.Property(x => x.Evidencia).HasMaxLength(2000);
            e.Property(x => x.TipoContenido).HasMaxLength(100);
            e.Property(x => x.EstadoIA).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ProbabilidadFalsoPositivo).HasPrecision(5, 4);
            e.Property(x => x.JustificacionIA).HasMaxLength(500);
            e.Property(x => x.VersionModeloIA).HasMaxLength(40);
            e.Ignore(x => x.EsPosibleFalsoPositivo);
            e.HasOne<Escaneo>().WithMany().HasForeignKey(x => x.EscaneoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.EscaneoId, x.Severidad }).IsDescending(false, true);
            e.HasIndex(x => x.EstadoIA);
        });

        mb.Entity<Alerta>(e =>
        {
            e.ToTable("alertas");
            e.HasKey(x => x.Id);
            e.HasOne<Escaneo>().WithMany().HasForeignKey(x => x.EscaneoId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.EscaneoId).IsUnique(); // una alerta por escaneo
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.DestinatarioId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.CorreoDestino).HasMaxLength(254).IsRequired();
            e.Property(x => x.Resumen).HasMaxLength(1000).IsRequired();
            e.Property(x => x.EstadoEnvio).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.UltimoError).HasMaxLength(500);
            e.HasIndex(x => new { x.EstadoEnvio, x.ProximoIntentoEn });
        });

        mb.Entity<RegistroAuditoria>(e =>
        {
            e.ToTable("registros_auditoria");
            e.HasKey(x => x.Id);
            e.Property(x => x.CorreoIntentado).HasMaxLength(254);
            e.Property(x => x.Accion).HasMaxLength(60).IsRequired();
            e.Property(x => x.Recurso).HasMaxLength(200);
            e.Property(x => x.Resultado).HasMaxLength(20).IsRequired();
            e.Property(x => x.IpOrigen).HasMaxLength(45);
            e.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Fecha);
        });
    }
}

/// <summary>Usado solo por "dotnet ef migrations".</summary>
public sealed class FabricaDisenoEgidaDbContext : IDesignTimeDbContextFactory<EgidaDbContext>
{
    public EgidaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EgidaDbContext>()
            .UseNpgsql("Host=localhost;Database=egida;Username=egida;Password=diseno")
            .UseSnakeCaseNamingConvention()
            .Options);
}
