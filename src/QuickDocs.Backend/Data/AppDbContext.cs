using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models; // Aquí traemos nuestros modelos compartidos

namespace QuickDocs.Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tablas de la Base de Datos
        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Item> Items { get; set; }

        // --- Seguridad e Identidad ---
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Perfil> Perfiles { get; set; }

        // --- Documentos (Cabeceras) ---
        // Registramos la clase madre. EF usará esto para armar la herencia.
        public DbSet<Documento> Documentos { get; set; } 
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<Remito> Remitos { get; set; }
        public DbSet<Recibo> Recibos { get; set; }
        public DbSet<NotaCredito> NotasCredito { get; set; }

        // --- Detalles (Renglones) ---
        public DbSet<DetallePresupuesto> DetallesPresupuesto { get; set; }
        public DbSet<DetalleRemito> DetallesRemito { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Aquí podemos configurar reglas adicionales, por ejemplo:
            // que el nombre del cliente sea obligatorio
            modelBuilder.Entity<Cliente>().Property(c => c.Nombre).IsRequired();
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Perfil)
                .WithOne(p => p.Usuario)
                .HasForeignKey<Perfil>(p => p.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade); // Si se borra el usuario, se borra el perfil

            // ==========================================
            // CONFIGURACIÓN: TPH PARA DOCUMENTOS
            // ==========================================
            // EF ya sabe qué hacer por defecto, pero lo dejamos explícito para que sea más legible
            modelBuilder.Entity<Documento>()
                .HasDiscriminator<string>("Discriminador")
                .HasValue<Presupuesto>("Presupuesto")
                .HasValue<Remito>("Remito")
                .HasValue<Recibo>("Recibo")
                .HasValue<NotaCredito>("NotaCredito");

            // ==========================================
            // CONFIGURACIÓN: RELACIONES DE DETALLES
            // ==========================================
            
            // Un Presupuesto tiene muchos Detalles, y cada Detalle pertenece a un Presupuesto
            modelBuilder.Entity<Presupuesto>()
                .HasMany(p => p.Detalles)
                .WithOne()
                .HasForeignKey(d => d.PresupuestoId)
                .OnDelete(DeleteBehavior.Cascade);

            // Un Remito tiene muchos Detalles, y cada Detalle pertenece a un Remito
            modelBuilder.Entity<Remito>()
                .HasMany(r => r.Detalles)
                .WithOne()
                .HasForeignKey(d => d.RemitoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}