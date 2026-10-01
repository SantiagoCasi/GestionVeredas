using SistemaVeredas.Models;
using SistemaVeredas.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SistemaVeredas.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vereda> Veredas { get; set; }
        public DbSet<Rotura> Roturas { get; set; }
        public DbSet<TipoSuelo> TiposSuelo { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Paquete> Paquetes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Los enums se guardan como texto ("FaltaMedir") y no como número.
            configurationBuilder.Properties<Estado>().HaveConversion<string>().HaveMaxLength(30);
            configurationBuilder.Properties<Prioridad>().HaveConversion<string>().HaveMaxLength(10);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // El email se usa para iniciar sesión: no puede repetirse.
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.UsEmail)
                .IsUnique();

            // Proveedor 1—N Paquete: no se puede borrar un proveedor que tiene paquetes.
            modelBuilder.Entity<Paquete>()
                .HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Paquetes)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Proveedor 1—N Vereda (opcional): no se puede borrar un proveedor que tiene veredas.
            modelBuilder.Entity<Vereda>()
                .HasOne(v => v.Proveedor)
                .WithMany(pr => pr.Veredas)
                .HasForeignKey(v => v.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Paquete 1—N Vereda (opcional): al borrar un paquete, sus veredas quedan sin paquete.
            modelBuilder.Entity<Vereda>()
                .HasOne(v => v.Paquete)
                .WithMany(p => p.Veredas)
                .HasForeignKey(v => v.PaqueteId)
                .OnDelete(DeleteBehavior.SetNull);

            // Vereda 1—N Rotura: al borrar una vereda se borran sus roturas.
            modelBuilder.Entity<Rotura>()
                .HasOne(r => r.Vereda).WithMany(v => v.Roturas)
                .HasForeignKey(r => r.VeredaId)
                .OnDelete(DeleteBehavior.Cascade);

            // TipoSuelo 1—N Rotura: no se puede borrar un tipo de suelo en uso (RN-13).
            modelBuilder.Entity<Rotura>()
                .HasOne(r => r.TipoSuelo).WithMany(t => t.Roturas)
                .HasForeignKey(r => r.TipoSueloId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
