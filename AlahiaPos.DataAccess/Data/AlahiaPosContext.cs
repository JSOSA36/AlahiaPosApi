using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
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
        public DbSet<BizcochoEncargo> BizcochoEncargo { get; set; }
        public DbSet<PagoEmpresa> PagosEmpresa { get; set; }
        public DbSet<SuscripcionCiclo> SuscripcionCiclo { get; set; }
        public DbSet<SuscripcionCicloDetalle> SuscripcionCicloDetalle { get; set; }
        public DbSet<SuscripcionEvento> SuscripcionEvento { get; set; }
        public DbSet<SuscripcionAvisoLog> SuscripcionAvisoLog { get; set; }
        public DbSet<EmpresaCargoRecurrente> EmpresaCargoRecurrente { get; set; }
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
        public DbSet<DescuentoCategoriaDetalle> DescuentoCategoriaDetalle { get; set; }
        public DbSet<EmpresaModulo> Empresa_Modulos { get; set; }
        public DbSet<Modulo> Modulos { get; set; }
        public DbSet<PlanesCloud> PlanesCloud { get; set; }
        public DbSet<MovimientosInventario>
         MovimientosInventario
        { get; set; }

        public DbSet<MovimientosInventarioDetalle>
            MovimientosInventarioDetalle
        { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Perfiles> Perfiles { get; set; }
        public DbSet<PerfilRoles> PerfilRoles { get; set; }
        public DbSet<CuentaFinanciera> CuentaFinanciera { get; set; }
        public DbSet<MovimientoFinanciero> MovimientoFinanciero { get; set; }
        public DbSet<MetodoPagoCuenta> MetodoPagoCuenta { get; set; }
        public DbSet<CajaCierre> CajaCierre { get; set; }
        public DbSet<CajaApertura> CajaApertura { get; set; }
        public DbSet<CajaMovimiento> CajaMovimiento { get; set; }
        public DbSet<SecuenciaDocumentos> SecuenciaDocumentos { get; set; }
        public DbSet<OrdenCompraHeader> OrdenCompraHeaders { get; set; }
        public DbSet<OrdenCompraDetalle> OrdenCompraDetalles { get; set; }
        public DbSet<ActivoFijo> ActivosFijos { get; set; }
        public DbSet<PoliticasVersion> PoliticasVersion { get; set; }
        public DbSet<PoliticasAceptacion> PoliticasAceptacion { get; set; }
        public DbSet<Proveedores> Proveedores { get; set; }
        public DbSet<PagosProveedor> PagosProveedor { get; set; }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketMensaje> TicketMensajes { get; set; }
        public DbSet<TicketAdjunto> TicketAdjuntos { get; set; }
        public DbSet<TicketNotificacion> TicketNotificaciones { get; set; }
        public DbSet<TicketSecuencia> TicketSecuencia { get; set; }

        public DbSet<Notificacion> Notificaciones { get; set; }
        public DbSet<NotificacionCanalLog> NotificacionCanalLog { get; set; }

        // ==============================
        // CENTRO DE PRODUCCIÓN
        // ==============================
        public DbSet<ProduccionTipoTrabajo> ProduccionTipoTrabajo { get; set; }
        public DbSet<ProduccionConfiguracionEmpresa> ProduccionConfiguracionEmpresa { get; set; }
        public DbSet<ProduccionFlujo> ProduccionFlujo { get; set; }
        public DbSet<ProduccionFlujoEstado> ProduccionFlujoEstado { get; set; }
        public DbSet<ProduccionFlujoTransicion> ProduccionFlujoTransicion { get; set; }
        public DbSet<ProduccionEstacion> ProduccionEstacion { get; set; }
        public DbSet<ProduccionEstacionResponsable> ProduccionEstacionResponsable { get; set; }
        public DbSet<ProduccionTrabajo> ProduccionTrabajo { get; set; }
        public DbSet<ProduccionTrabajoItem> ProduccionTrabajoItem { get; set; }
        public DbSet<ProduccionHistorial> ProduccionHistorial { get; set; }

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
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<AlmacenExistencia> AlmacenExistencia { get; set; }
        public DbSet<ConduceHeader> ConduceHeader { get; set; }
        public DbSet<ConduceDetalle> ConduceDetalle { get; set; }
        public DbSet<DgiiCatalogo> DgiiCatalogo { get; set; }
        public DbSet<DgiiConfiguracionEmpresa> DgiiConfiguracionEmpresa { get; set; }
        public DbSet<DgiiConfiguracionAuditoria> DgiiConfiguracionAuditoria { get; set; }
        public DbSet<NotasCredito> NotasCredito { get; set; }
        public DbSet<NotasCreditoDetalle> NotasCreditoDetalle { get; set; }
        public DbSet<PlantillasDocumentosClinicos> PlantillasDocumentosClinicos { get; set; }
        public DbSet<DocumentosClinicos> DocumentosClinicos { get; set; }
        public DbSet<CuentaContable> CuentasContables { get; set; }
        public DbSet<AsientoContable> AsientosContables { get; set; }
        public DbSet<AsientoContableDetalle> AsientosContablesDetalle { get; set; }
        public DbSet<PeriodoContable> PeriodosContables { get; set; }
        public DbSet<ContabilidadConfiguracion> ContabilidadConfiguracion { get; set; }
        public DbSet<EventoOutbox> EventosOutbox { get; set; }
        public DbSet<ContabilidadIntegracionLog> ContabilidadIntegracionLog { get; set; }
        public DbSet<ContabilidadCuentaMapeo> ContabilidadCuentaMapeo { get; set; }
        public DbSet<TesoreriaTipoCuenta> TesoreriaTipoCuenta { get; set; }
        public DbSet<TesoreriaSubtipoCuenta> TesoreriaSubtipoCuenta { get; set; }
        public DbSet<TesoreriaConfiguracion> TesoreriaConfiguracion { get; set; }
        public DbSet<TesoreriaCuentaContableMapeo> TesoreriaCuentaContableMapeo { get; set; }
        public DbSet<TesoreriaTipoDocumento> TesoreriaTipoDocumento { get; set; }
        public DbSet<TesoreriaConciliacion> TesoreriaConciliacion { get; set; }
        public DbSet<TesoreriaConciliacionLinea> TesoreriaConciliacionLinea { get; set; }

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

            // 🔥 Documentos clínicos → Cliente (PK: IDCliente)
            modelBuilder.Entity<DocumentosClinicos>()
                .HasOne(d => d.Cliente)
                .WithMany()
                .HasForeignKey(d => d.IdCliente)
                .HasPrincipalKey(c => c.IDCliente);

            modelBuilder.Entity<OrdenCompraDetalle>(e =>
            {
                e.Ignore(d => d.Productos);
                e.Property(d => d.IdProducto).HasColumnName("IdProducto");
            });
        }
    }
}