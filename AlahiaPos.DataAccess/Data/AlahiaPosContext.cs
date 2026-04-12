using AlahiaPos.Entities.Domain;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Data
{
    public class AlahiaPosContext : DbContext
    {
        public AlahiaPosContext(DbContextOptions<AlahiaPosContext> options) : base(options)
        {
        }

        // ==============================
        // 🔹 EXISTENTES
        // ==============================

        public DbSet<Mesas> Mesas { get; set; }
        public DbSet<Cocinas> Cocinas { get; set; }
        public DbSet<Zonas> Zonas { get; set; }
        public DbSet<ClientesDGII> ClientesDGII { get; set; }
        public DbSet<ParametrosConfigs> ParametrosConfigs { get; set; }
        public DbSet<ImpresorasZonas> ImpresorasZonas { get; set; }
        public DbSet<Empleados> EmpleadosP { get; set; }
        public DbSet<PagosFacturasClientes> PagosFacturasClientes { get; set; }
        public DbSet<Empresas> Empresas { get; set; }
        public DbSet<Gastos> Gastos { get; set; }
        public DbSet<FacturaDetalles> FacturaDetalles { get; set; }
        public DbSet<FacturaHeaders> FacturaHeaders { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<EmpleadoAreaComision> EmpleadoServicioComisions { get; set; }
        public DbSet<AreaServicio> Servicios { get; set; }
        public DbSet<Cita> Citas { get; set; }
       
        public DbSet<HorariosEstilista> HorariosEstilistas { get; set; }
        public DbSet<DescuentoHeader> DescuentoHeader { get; set; }
        public DbSet<DescuentoDetalle> DescuentoDetalle { get; set; }
        public DbSet<Ingresos> Ingresos { get; set; }
        public DbSet<PasswordResetToken> PasswordResetToken { get; set; }
        public DbSet<DescuentoAreaDetalle> DescuentoAreaDetalle { get; set; }
        public DbSet<EmpresaModulo> Empresa_Modulos { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<PlanesCloud> PlanesCloud { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Perfiles> Perfiles { get; set; }
        public DbSet<PerfilRoles> PerfilRoles { get; set; }

        // ==============================
        // 🔥 FACTURACIÓN ELECTRÓNICA
        // ==============================

        public DbSet<CertificadoDigital> CertificadosDigitales { get; set; }
        public DbSet<ECFEncabezado> ECFEncabezados { get; set; }
        public DbSet<ECFDetalle> ECFDetalles { get; set; }
        public DbSet<ECFXml> ECFXmls { get; set; }
        public DbSet<ECFHistorialEstado> ECFHistorialEstados { get; set; }
        public DbSet<SecuenciaECF> SecuenciasECF { get; set; }
        public DbSet<Parametros> Parametros { get; set; }
        public DbSet<LavadorConsumo> LavadorConsumo { get; set; }
        public DbSet<AreaNegocio> AreaNegocio { get; set; }

        // ==============================
        // 🔧 CONFIGURACIONES RELACIONES
        // ==============================

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔥 Relación ECFEncabezado → Detalles
            modelBuilder.Entity<ECFEncabezado>()
                .HasMany(e => e.Detalles)
                .WithOne(d => d.ECFEncabezado)
                .HasForeignKey(d => d.IdECF)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔥 Relación ECFEncabezado → XMLs
            modelBuilder.Entity<ECFEncabezado>()
                .HasMany(e => e.Xmls)
                .WithOne(x => x.ECFEncabezado)
                .HasForeignKey(x => x.IdECF)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔥 Relación ECFEncabezado → Historial
            modelBuilder.Entity<ECFEncabezado>()
                .HasMany(e => e.HistorialEstados)
                .WithOne(h => h.ECFEncabezado)
                .HasForeignKey(h => h.IdECF)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔥 Empresa → Certificados
            modelBuilder.Entity<CertificadoDigital>()
                .HasOne(c => c.Empresa)
                .WithMany(e => e.CertificadosDigitales)
                .HasForeignKey(c => c.IdEmpresa);

            // 🔥 Empresa → Secuencias
            modelBuilder.Entity<SecuenciaECF>()
                .HasOne(s => s.Empresa)
                .WithMany(e => e.SecuenciasECF)
                .HasForeignKey(s => s.IdEmpresa);

            // 🔥 Índice único recomendado (empresa + tipoECF)
            modelBuilder.Entity<SecuenciaECF>()
                .HasIndex(s => new { s.IdEmpresa, s.TipoNCF })
                .IsUnique();
        }
    }
}