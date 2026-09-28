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
        public DbSet<SuscripcionCuentaCobro> SuscripcionCuentaCobro { get; set; }
        public DbSet<EmpresaCargoRecurrente> EmpresaCargoRecurrente { get; set; }
        public DbSet<Cocinas> Cocinas { get; set; }
        public DbSet<Zonas> Zonas { get; set; }
        public DbSet<ClientesDGII> ClientesDGII { get; set; }
        public DbSet<ParametrosConfigs> ParametrosConfigs { get; set; }
        public DbSet<ImpresorasZonas> ImpresorasZonas { get; set; }
        public DbSet<Empleados> EmpleadosP { get; set; }
        public DbSet<EmpleadoLaboral> EmpleadoLaboral { get; set; }
        public DbSet<EmpleadoFichaPersonal> EmpleadoFichaPersonal { get; set; }
        public DbSet<EmpleadoFamiliar> EmpleadoFamiliar { get; set; }
        public DbSet<EmpleadoSalarioHistorial> EmpleadoSalarioHistorial { get; set; }
        public DbSet<NominaConcepto> NominaConcepto { get; set; }
        public DbSet<NominaConceptoAsignacion> NominaConceptoAsignacion { get; set; }
        public DbSet<RrhhDepartamento> RrhhDepartamento { get; set; }
        public DbSet<RrhhCargo> RrhhCargo { get; set; }
        public DbSet<RrhhBeneficio> RrhhBeneficio { get; set; }
        public DbSet<RrhhCargoBeneficio> RrhhCargoBeneficio { get; set; }
        public DbSet<RrhhCargoSalarioHistorial> RrhhCargoSalarioHistorial { get; set; }
        public DbSet<RrhhJornada> RrhhJornada { get; set; }
        public DbSet<RrhhJornadaDia> RrhhJornadaDia { get; set; }
        public DbSet<RrhhTurno> RrhhTurno { get; set; }
        public DbSet<RrhhEmpleadoHorario> RrhhEmpleadoHorario { get; set; }
        public DbSet<RrhhPonchada> RrhhPonchada { get; set; }
        public DbSet<RrhhPonchadaCorreccion> RrhhPonchadaCorreccion { get; set; }
        public DbSet<RrhhEmpleadoRostro> RrhhEmpleadoRostro { get; set; }
        public DbSet<RrhhKioscoEvento> RrhhKioscoEvento { get; set; }
        public DbSet<RrhhPonchadorDispositivo> RrhhPonchadorDispositivo { get; set; }
        public DbSet<RrhhPonchadorPersona> RrhhPonchadorPersona { get; set; }
        public DbSet<RrhhPonchadorIngesta> RrhhPonchadorIngesta { get; set; }
        public DbSet<RrhhTipoAusencia> RrhhTipoAusencia { get; set; }
        public DbSet<RrhhSolicitudAusencia> RrhhSolicitudAusencia { get; set; }
        public DbSet<RrhhAsistenciaDia> RrhhAsistenciaDia { get; set; }
        public DbSet<RrhhPrestamo> RrhhPrestamo { get; set; }
        public DbSet<RrhhPrestamoCuota> RrhhPrestamoCuota { get; set; }
        public DbSet<RrhhAnticipo> RrhhAnticipo { get; set; }
        public DbSet<NominaProceso> NominaProceso { get; set; }
        public DbSet<NominaProcesoEmpleado> NominaProcesoEmpleado { get; set; }
        public DbSet<NominaProcesoEvento> NominaProcesoEvento { get; set; }
        public DbSet<PagosFacturasClientes> PagosFacturasClientes { get; set; }
        public DbSet<Empresas> Empresas { get; set; }
        public DbSet<Gastos> Gastos { get; set; }
        public DbSet<CategoriaGasto> CategoriasGasto { get; set; }
        public DbSet<FacturaDetalles> FacturaDetalles { get; set; }
        public DbSet<FacturaHeaders> FacturaHeaders { get; set; }
        public DbSet<CargoPagoRegla> CargoPagoReglas { get; set; }
        public DbSet<FacturaCargo> FacturaCargos { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<Guarnicion> Guarniciones { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Area> Areas { get; set; }
        public DbSet<EmpleadoAreaComision> EmpleadoServicioComisions { get; set; }
        public DbSet<AreaServicio> Servicios { get; set; }
        public DbSet<Cita> Citas { get; set; }
        public DbSet<WhatsAppCitasConsumo> WhatsAppCitasConsumo { get; set; }
       
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
        public DbSet<PosTerminal> PosTerminal { get; set; }
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
        public DbSet<PedidoOnlineCanal> PedidoOnlineCanal { get; set; }
        public DbSet<PedidoOnline> PedidoOnline { get; set; }
        public DbSet<DeliveryAsignacion> DeliveryAsignacion { get; set; }
        public DbSet<DeliveryRepartidor> DeliveryRepartidor { get; set; }

        // ==============================
        // 🔥 FACTURACIÓN ELECTRÓNICA
        // ==============================

        public DbSet<CertificadoDigital> CertificadosDigitales { get; set; }
        public DbSet<CertecfSesion> CertecfSesiones { get; set; }
        public DbSet<CertecfCaso> CertecfCasos { get; set; }
        public DbSet<CertecfInboundLog> CertecfInboundLogs { get; set; }
        public DbSet<ECFEncabezado> ECFEncabezados { get; set; }
        public DbSet<ECFDetalle> ECFDetalles { get; set; }
        public DbSet<ECFXml> ECFXmls { get; set; }
        public DbSet<ECFHistorialEstado> ECFHistorialEstados { get; set; }
        public DbSet<SecuenciaECF> SecuenciasECF { get; set; }
        public DbSet<SecuenciaECFAsignacion> SecuenciaECFAsignaciones { get; set; }
        public DbSet<Parametros> Parametros { get; set; }
        public DbSet<LavadorConsumo> LavadorConsumo { get; set; }
        public DbSet<AreaNegocio> AreaNegocio { get; set; }
        public DbSet<Almacen> Almacenes { get; set; }
        public DbSet<AlmacenExistencia> AlmacenExistencia { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<UsuarioSucursal> UsuarioSucursales { get; set; }
        public DbSet<SucursalCambioLog> SucursalCambioLogs { get; set; }
        public DbSet<RecetaProduccion> RecetaProduccion { get; set; }
        public DbSet<RecetaProduccionItem> RecetaProduccionItem { get; set; }
        public DbSet<OrdenProduccion> OrdenProduccion { get; set; }
        public DbSet<OrdenProduccionMaterial> OrdenProduccionMaterial { get; set; }
        public DbSet<ConduceHeader> ConduceHeader { get; set; }
        public DbSet<ConduceDetalle> ConduceDetalle { get; set; }
        public DbSet<DgiiCatalogo> DgiiCatalogo { get; set; }
        public DbSet<DgiiConfiguracionEmpresa> DgiiConfiguracionEmpresa { get; set; }
        public DbSet<EmpresaAiConfig> EmpresaAiConfig { get; set; }
        public DbSet<DgiiConfiguracionAuditoria> DgiiConfiguracionAuditoria { get; set; }
        public DbSet<NotasCredito> NotasCredito { get; set; }
        public DbSet<NotasCreditoDetalle> NotasCreditoDetalle { get; set; }
        public DbSet<NotasCreditoAplicacion> NotasCreditoAplicaciones { get; set; }
        public DbSet<ClienteSaldoAFavor> ClienteSaldoAFavor { get; set; }
        public DbSet<PlantillasDocumentosClinicos> PlantillasDocumentosClinicos { get; set; }
        public DbSet<DocumentosClinicos> DocumentosClinicos { get; set; }
        public DbSet<FichasClinicas> FichasClinicas { get; set; }
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
        public DbSet<TesoreriaConciliacionAuditoria> TesoreriaConciliacionAuditoria { get; set; }
        public DbSet<TesoreriaExtractoImport> TesoreriaExtractoImport { get; set; }
        public DbSet<TesoreriaExtractoLinea> TesoreriaExtractoLinea { get; set; }
        public DbSet<PagoReclasificacion> PagoReclasificacion { get; set; }
        public DbSet<ArsAseguradora> ArsAseguradoras { get; set; }

        // ==============================
        // COTIZADOR COMERCIAL
        // ==============================
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.ModuloComercial> ModuloComercial { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.ModuloDependencia> ModuloDependencia { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.TipoNegocio> TipoNegocio { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.ModuloTipoNegocio> ModuloTipoNegocio { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.CotizadorParametro> CotizadorParametro { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.CotizadorTramoDocumento> CotizadorTramoDocumento { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.Cotizacion> Cotizacion { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.CotizacionDetalle> CotizacionDetalle { get; set; }
        public DbSet<AlahiaPos.Entities.Domain.Cotizador.CotizacionLead> CotizacionLead { get; set; }

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

            // 🔥 Empresa → Certificados (tabla real: CertificadoDigital, no el nombre del DbSet)
            modelBuilder.Entity<CertificadoDigital>()
                .ToTable("CertificadoDigital")
                .HasOne(c => c.Empresa)
                .WithMany(e => e.CertificadosDigitales)
                .HasForeignKey(c => c.IdEmpresa);

            modelBuilder.Entity<CertecfSesion>(e =>
            {
                e.HasMany(s => s.Casos)
                    .WithOne(c => c.Sesion)
                    .HasForeignKey(c => c.IdSesion)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(s => new { s.IdEmpresa, s.FechaCreacion });
            });

            modelBuilder.Entity<CertecfInboundLog>(e =>
            {
                e.HasIndex(x => new { x.Rnc, x.Fecha });
            });

            // 🔥 Empresa → Secuencias
            modelBuilder.Entity<SecuenciaECF>()
                .HasOne(s => s.Empresa)
                .WithMany(e => e.SecuenciasECF)
                .HasForeignKey(s => s.IdEmpresa);

            modelBuilder.Entity<SecuenciaECF>()
                .HasOne(s => s.Sucursal)
                .WithMany()
                .HasForeignKey(s => s.IdSucursal)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SecuenciaECFAsignacion>(e =>
            {
                e.ToTable("SecuenciaECFAsignacion");
                e.HasOne(a => a.Secuencia)
                    .WithMany(s => s.Asignaciones)
                    .HasForeignKey(a => a.IdSecuencia)
                    .OnDelete(DeleteBehavior.Restrict);
                e.HasOne(a => a.Sucursal)
                    .WithMany()
                    .HasForeignKey(a => a.IdSucursal)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 🔥 Documentos clínicos → Cliente (PK: IDCliente)
            modelBuilder.Entity<DocumentosClinicos>()
                .HasOne(d => d.Cliente)
                .WithMany()
                .HasForeignKey(d => d.IdCliente)
                .HasPrincipalKey(c => c.IDCliente);

            modelBuilder.Entity<FichasClinicas>()
                .HasOne(f => f.Cliente)
                .WithMany()
                .HasForeignKey(f => f.IdCliente)
                .HasPrincipalKey(c => c.IDCliente);

            modelBuilder.Entity<FichasClinicas>()
                .HasIndex(f => new { f.IdEmpresa, f.IdCliente })
                .IsUnique();

            modelBuilder.Entity<OrdenCompraDetalle>(e =>
            {
                e.Ignore(d => d.Productos);
                e.Property(d => d.IdProducto).HasColumnName("IdProducto");
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.ModuloComercial>(e =>
            {
                e.HasIndex(x => x.ModuloId).IsUnique();
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.ModuloDependencia>(e =>
            {
                e.HasIndex(x => new { x.ModuloId, x.ModuloRequeridoId, x.Tipo }).IsUnique();
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.TipoNegocio>(e =>
            {
                e.HasIndex(x => x.Codigo).IsUnique();
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.ModuloTipoNegocio>(e =>
            {
                e.HasIndex(x => new { x.ModuloId, x.TipoNegocioId }).IsUnique();
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.CotizadorParametro>(e =>
            {
                e.HasIndex(x => x.Clave).IsUnique();
            });

            modelBuilder.Entity<AlahiaPos.Entities.Domain.Cotizador.Cotizacion>(e =>
            {
                e.HasIndex(x => x.Folio).IsUnique();
            });

            modelBuilder.Entity<RrhhJornada>()
                .HasMany(j => j.Dias)
                .WithOne(d => d.Jornada)
                .HasForeignKey(d => d.IdJornada)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RrhhCargo>()
                .HasMany(c => c.Beneficios)
                .WithOne(b => b.Cargo)
                .HasForeignKey(b => b.IdCargo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RrhhPrestamo>()
                .HasMany(p => p.CuotasDetalle)
                .WithOne(c => c.Prestamo)
                .HasForeignKey(c => c.IdPrestamo)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NominaProceso>()
                .HasMany(p => p.Empleados)
                .WithOne(e => e.Proceso)
                .HasForeignKey(e => e.IdNominaProceso)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NominaProceso>()
                .HasMany(p => p.Eventos)
                .WithOne()
                .HasForeignKey(e => e.IdNominaProceso)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CargoPagoRegla>(e =>
            {
                e.ToTable("CargoPagoRegla");
                e.Property(x => x.MetodosVinculados)
                    .HasMaxLength(2000)
                    .HasConversion(
                        v => string.Join("|", (v ?? new List<string>())
                            .Select(s => (s ?? "").Trim())
                            .Where(s => s.Length > 0)
                            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.First())),
                        v => string.IsNullOrWhiteSpace(v)
                            ? new List<string>()
                            : v.Split(new[] { '|', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => s.Length > 0)
                                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                                .Select(g => g.First())
                                .ToList());
            });

            modelBuilder.Entity<PosTerminal>(e =>
            {
                e.ToTable("PosTerminal");
                e.HasIndex(t => new { t.IdEmpresa, t.Estado });
            });

            modelBuilder.Entity<AlmacenExistencia>(e =>
            {
                e.ToTable("AlmacenExistencias");
                e.HasIndex(x => new { x.IdAlmacen, x.IdProducto }).IsUnique();
            });

            modelBuilder.Entity<Sucursal>(e =>
            {
                e.ToTable("Sucursal");
                e.HasIndex(s => new { s.IdEmpresa, s.Codigo }).IsUnique();
            });

            modelBuilder.Entity<UsuarioSucursal>(e =>
            {
                e.ToTable("UsuarioSucursal");
                e.HasIndex(x => new { x.IdUsuario, x.IdSucursal }).IsUnique();
            });

            modelBuilder.Entity<SucursalCambioLog>(e =>
            {
                e.ToTable("SucursalCambioLog");
            });
        }
    }
}