using Microsoft.EntityFrameworkCore;

namespace Servicio2.Models.DbModels
{
    public class Context:DbContext
    {
      
        public Context(DbContextOptions options):base(options) { }
        public DbSet<roles> Rol { get; set; }
        public DbSet<Empleados> Empleados { get; set; }
        public DbSet<Solicitud> solicitud {  get; set; }
        public DbSet<HistorialEstatus> HistorialEstatus { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<roles>().ToTable("roles");
            modelBuilder.Entity<Empleados>().ToTable("empleados");
            modelBuilder.Entity<Solicitud>()
                .Property(x => x.Estatus)
                .HasConversion<string>();
            modelBuilder.Entity<Solicitud>()
                .Property(x => x.Tipo).HasConversion<string>();

            modelBuilder.Entity<HistorialEstatus>()
                 .Property(x => x.EstatusAnterior)
                 .HasConversion<string>();
            modelBuilder.Entity<HistorialEstatus>() 
                .Property(x=>x.EstatusNuevo)
                .HasConversion<string>();
        }

    }
}
