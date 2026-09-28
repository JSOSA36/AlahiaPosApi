using Alahia_Pos.Services;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Repository;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Setting;
using AlahiaPos.Payroll.Infrastructure;
using AlahiaPosApi;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;
using System.Text.Json;
using PrinterLibrary;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Controllers + Swagger
// =============================
builder.Services.AddScoped<ISesionTokenResolver, SesionTokenResolver>();
builder.Services.AddScoped<SesionAuthFilter>();
builder.Services.AddScoped<RequiereTerminalPosFilter>();
// ERP vivo: sesión token + cupo PC desactivados (no reactivar por appsettings del IIS).
builder.Services.AddControllers(options =>
    {
        if (ErpVivoAuth.RequerirSesion)
            options.Filters.Add<SesionAuthFilter>();
        if (ErpVivoAuth.RequerirTerminalPos)
            options.Filters.Add<RequiereTerminalPosFilter>();
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// =============================
// DbContext
// =============================
builder.Services.AddDbContext<AlahiaPosContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// =============================
// Repositorios
// =============================
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

// =============================
// Servicios de negocio (los tuyos)
// =============================
builder.Services.AddScoped<IMesas, IMesasServices>();
builder.Services.AddScoped<ICategorias, CategoriaServices>();
builder.Services.AddScoped<IProductos, IProductosServices>();
builder.Services.AddScoped<IFacturaDetalle, IFacturaDetalleServices>();
builder.Services.AddScoped<IFacturaHeader, IFacturaHeaderServices>();
builder.Services.AddScoped<IZonas, IZonasServices>();
builder.Services.AddScoped<IClientes, ClienteServices>();
builder.Services.AddScoped<IProveedores, ProveedoresServices>();
builder.Services.AddScoped<IOrdenCompraHeader, OrdenCompraHeaderServices>();
builder.Services.AddScoped<IComprasService, ComprasService>();
builder.Services.AddScoped<IFacturaCompraImagenService, FacturaCompraImagenService>();
builder.Services.AddScoped<IActivosFijosService, ActivosFijosService>();
builder.Services.AddScoped<IPoliticasServicioService, PoliticasServicioService>();
builder.Services.AddScoped<IDashboardGerencialService, DashboardGerencialService>();
builder.Services.AddScoped<IGastos, GastosServices>();
builder.Services.AddScoped<ICategoriaGastoService, CategoriaGastoService>();
builder.Services.AddScoped<IEmpresas, EmpresaServices>();
builder.Services.AddScoped<ICocinas, CocinaServices>();
builder.Services.AddScoped<IParametroConfig, IParametroCOnfigServices>();
builder.Services.AddScoped<IValidateIMpuesto, ValidateImpuestos>();
builder.Services.AddScoped<IAreas, AreaServices>();
builder.Services.AddScoped<IAlmacenes, AlmacenesServices>();
builder.Services.AddScoped<IAlmacenExistencia, AlmacenExistenciaServices>();
builder.Services.AddScoped<IEmpleadoAreaComisionService, EmpleadoAreaComisionService>();
builder.Services.AddScoped<ICitas, CitaServices>();
builder.Services.AddScoped<ICitasPublicasService, CitasPublicasService>();
builder.Services.AddScoped<IHorariosEstilista, HorarioEstilistaServices>();
builder.Services.AddScoped<IRNCService, RNCService>();
builder.Services.AddScoped<IDescuentoHeader, DescuentoHeaderServices>();
builder.Services.AddScoped<IDescuentoDetalle, DescuentoDetalleServices>();
builder.Services.AddScoped<IPagosFacturasClientes, PagosFacturasClientesService>();
builder.Services.AddScoped<IArsAseguradoraService, ArsAseguradoraService>();
builder.Services.AddScoped<IAntiguedadSaldosService, AntiguedadSaldosService>();
builder.Services.AddScoped<IConducesService, ConducesService>();
builder.Services.AddScoped<IIngresos, IngresosService>();
builder.Services.AddScoped<IDescuentoAreaDetalle, DescuentoAreaDetalleService>();
builder.Services.AddScoped<INCF_Secuencias, NCF_SecuenciasServices>();
builder.Services.AddScoped<INotification, NotificationServices>();
builder.Services.AddScoped<IModulo, ModulosServices>();
builder.Services.AddScoped<IEmpresaModulos, EmpresaModulosServices>();
builder.Services.AddScoped<IPlanesCloud, PlanesCloudService>();
builder.Services.AddScoped<ISecuenciaDocumentoService, SecuenciaDocumentoService>();
builder.Services.AddScoped<ICajaCierreService, CajaCierreServices>();
builder.Services.AddScoped<IReporteCruceStockService, ReporteCruceStockService>();
builder.Services.AddScoped<ICajaAperturaService, CajaAperturaServices>();
builder.Services.AddScoped<ICajaMovimientoService, CajaMovimientoServices>();
builder.Services.AddScoped<ICuentaFinancieraService, CuentaFinancieraService>();
builder.Services.AddScoped<ICuentaContableService, CuentaContableService>();
builder.Services.AddScoped<IAsientoContableService, AsientoContableService>();
builder.Services.AddScoped<IContabilidadLibrosService, ContabilidadLibrosService>();
builder.Services.AddScoped<IContabilidadReportesService, ContabilidadReportesService>();
builder.Services.AddScoped<IContabilidadCierreService, ContabilidadCierreService>();
builder.Services.AddScoped<IContabilidadCatalogoService, ContabilidadCatalogoService>();
builder.Services.AddScoped<IContabilidadIntegracionService, ContabilidadIntegracionService>();
builder.Services.AddScoped<IContabilidadGatekeeper, ContabilidadGatekeeper>();
builder.Services.AddScoped<IContabilidadCuentaMapeoService, ContabilidadCuentaMapeoService>();
builder.Services.AddScoped<ContabilidadOperacionContext>();
builder.Services.AddScoped<IContabilidadEventPublisher, ContabilidadEventPublisher>();
builder.Services.AddScoped<IContabilidadConfiguracionService, ContabilidadConfiguracionService>();
builder.Services.AddScoped<IContabilidadIntegracionLogService, ContabilidadIntegracionLogService>();
builder.Services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
builder.Services.AddScoped<IDomainEventHandler, ContabilidadEventHandler>();
builder.Services.AddScoped<IDomainEventHandler, AlahiaPos.DataAccess.Servicios.Produccion.ProduccionEventHandler>();
builder.Services.AddScoped<IProduccionConfiguracionService, AlahiaPos.DataAccess.Servicios.Produccion.ProduccionConfiguracionService>();
builder.Services.AddScoped<IProduccionFlujoService, AlahiaPos.DataAccess.Servicios.Produccion.ProduccionFlujoService>();
builder.Services.AddScoped<IProduccionTrabajoService, AlahiaPos.DataAccess.Servicios.Produccion.ProduccionTrabajoService>();
builder.Services.AddScoped<IProduccionPosAdapter, AlahiaPos.DataAccess.Servicios.Produccion.ProduccionPosAdapter>();
builder.Services.AddScoped<IPedidosOnlineService, AlahiaPos.DataAccess.Servicios.PedidosOnlineService>();
builder.Services.AddSingleton<IProduccionRealtime, AlahiaPosApi.Hubs.SignalRProduccionRealtime>();
// DgiiFiscalEventHandler YA NO se registra en el pipeline s?ncrono (Sprint B.1 ? worker async)

// Sprint B / B.1 ? capa fiscal DGII desacoplada
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IFiscalFeatureService, AlahiaPos.DataAccess.Servicios.Dgii.FiscalFeatureService>();
builder.Services.AddScoped<ITaxClassificationService, AlahiaPos.DataAccess.Servicios.Dgii.TaxClassificationService>();
builder.Services.AddScoped<IFiscalDocumentSnapshotService, AlahiaPos.DataAccess.Servicios.Dgii.FiscalDocumentSnapshotService>();
builder.Services.AddScoped<IDgiiFiscalService, AlahiaPos.DataAccess.Servicios.Dgii.DgiiFiscalService>();
builder.Services.AddScoped<IFiscalWorkEnqueueService, AlahiaPos.DataAccess.Servicios.Dgii.FiscalWorkEnqueueService>();
builder.Services.AddScoped<IFiscalOutboxProcessor, AlahiaPos.DataAccess.Servicios.Dgii.FiscalOutboxProcessor>();
builder.Services.AddScoped<IFiscalReconciliacionService, AlahiaPos.DataAccess.Servicios.Dgii.FiscalReconciliacionService>();
builder.Services.AddScoped<IDgiiConfigService, AlahiaPos.DataAccess.Servicios.Dgii.DgiiConfigService>();
builder.Services.AddScoped<IDgiiFiscalAuthService, AlahiaPos.DataAccess.Servicios.Dgii.DgiiFiscalAuthService>();
builder.Services.AddScoped<IReporte607Service, AlahiaPos.DataAccess.Servicios.Dgii.Reporte607Service>();
builder.Services.AddScoped<IReporteIt1Service, AlahiaPos.DataAccess.Servicios.Dgii.ReporteIt1Service>();
builder.Services.AddScoped<IReporteIr17Service, AlahiaPos.DataAccess.Servicios.Dgii.ReporteIr17Service>();
builder.Services.AddScoped<IReporteIr3Service, AlahiaPos.DataAccess.Servicios.Dgii.ReporteIr3Service>();
builder.Services.AddScoped<IReporteVentaService, AlahiaPos.DataAccess.Servicios.ReporteVentaService>();
builder.Services.AddScoped<ISalonService, AlahiaPos.DataAccess.Servicios.SalonService>();
builder.Services.AddHostedService<AlahiaPosApi.Workers.FiscalOutboxBackgroundService>();

// Facturaci?n Electr?nica ? m?dulo transversal
builder.Services.AddScoped<ISecuenciaEcfService, AlahiaPos.DataAccess.Servicios.FacturacionElectronica.SecuenciaEcfService>();
builder.Services.AddScoped<IDocumentoOrigenResolver, AlahiaPos.DataAccess.Servicios.FacturacionElectronica.PosDocumentoResolver>();
builder.Services.AddScoped<IDocumentoOrigenResolver, AlahiaPos.DataAccess.Servicios.FacturacionElectronica.NotaCreditoDocumentoResolver>();
builder.Services.AddScoped<IDocumentoOrigenResolverFactory, AlahiaPos.DataAccess.Servicios.FacturacionElectronica.DocumentoOrigenResolverFactory>();
builder.Services.AddScoped<IFacturacionElectronicaService, AlahiaPos.DataAccess.Servicios.FacturacionElectronica.FacturacionElectronicaService>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.FacturacionElectronica.ICertecfCertificacionService,
    AlahiaPos.DataAccess.Servicios.FacturacionElectronica.CertecfCertificacionService>();
builder.Services.AddHttpClient(nameof(AlahiaPos.DataAccess.Servicios.FacturacionElectronica.CertecfAcecfHttpSender));
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.FacturacionElectronica.ICertecfAcecfSender,
    AlahiaPos.DataAccess.Servicios.FacturacionElectronica.CertecfAcecfHttpSender>();

// Gateway Fiscal — ERP solo conoce IFiscalGateway; proveedor = FiscalGateway:BaseUrl + ApiKey
builder.Services.AddFiscalGateway(builder.Configuration);
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.FiscalGateway.EcfGatewayOutboxProcessor>();
builder.Services.AddHostedService<AlahiaPosApi.Workers.EcfGatewayBackgroundService>();

builder.Services.AddScoped<IMovimientoFinancieroService, MovimientoFinancieroService>();
builder.Services.AddScoped<IMetodoPagoCuentaService, MetodoPagoCuentaService>();
builder.Services.AddScoped<ITesoreriaConfiguracionService, TesoreriaConfiguracionService>();
builder.Services.AddScoped<ITesoreriaCuentaContableMapeoService, TesoreriaCuentaContableMapeoService>();
builder.Services.AddScoped<ITesoreriaConciliacionService, TesoreriaConciliacionService>();
builder.Services.AddScoped<ITesoreriaExtractoService, TesoreriaExtractoService>();
builder.Services.AddScoped<IPagoReclasificacionService, PagoReclasificacionService>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.ExtractosBancarios.IBankStatementAdapter,
    AlahiaPos.DataAccess.Servicios.ExtractosBancarios.ExcelStatementAdapter>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.ExtractosBancarios.IBankStatementAdapter,
    AlahiaPos.DataAccess.Servicios.ExtractosBancarios.PopularTextoStatementAdapter>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.ExtractosBancarios.IBankStatementAdapter,
    AlahiaPos.DataAccess.Servicios.ExtractosBancarios.CsvStatementAdapter>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.ExtractosBancarios.IBankStatementParserOrchestrator,
    AlahiaPos.DataAccess.Servicios.ExtractosBancarios.BankStatementParserOrchestrator>();
builder.Services.AddSingleton<IPdfTextExtractor, AlahiaPosApi.Servicios.ItextSharpPdfTextExtractor>();
// ======================================================
// ?? MOVIMIENTOS INVENTARIO
// ======================================================

builder.Services.AddScoped<
    IMovimientosInventarioService,
    MovimientosInventarioServices>();
builder.Services.AddScoped<INotasCredito, NotasCreditoServices>();
builder.Services.AddScoped<IUsuarios, UsuariosService>();
builder.Services.AddScoped<IPerfiles, PerfilesService>();
builder.Services.AddScoped<IPerfilRoles, PerfilRolesService>();
builder.Services.AddScoped<IDemoEmpresaBootstrap, DemoEmpresaBootstrapService>();
builder.Services.AddScoped<IEmpresaOperativaSeed, EmpresaOperativaSeedService>();
builder.Services.AddScoped<IEmpresaAdminService, EmpresaAdminService>();
builder.Services.AddScoped<IPosTerminalService, PosTerminalService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ISucursalService, SucursalService>();
builder.Services.AddScoped<ICargoPagoService, CargoPagoService>();
builder.Services.AddScoped<IEmpleados, EmpleadosService>();
        builder.Services.AddScoped<IEmpleadoLaboralService, EmpleadoLaboralService>();
        builder.Services.AddScoped<IEmpleadoFichaPersonalService, AlahiaPos.DataAccess.Servicios.Rrhh.EmpleadoFichaPersonalService>();
builder.Services.AddScoped<IRrhhCatalogoService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhCatalogoService>();
builder.Services.AddScoped<IRrhhPonchadorService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhPonchadorService>();
builder.Services.AddScoped<IRrhhKioscoService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhKioscoService>();
builder.Services.AddScoped<IRrhhDispositivoService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhDispositivoService>();
builder.Services.AddScoped<IRrhhAusenciaService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhAusenciaService>();
builder.Services.AddScoped<IRrhhAsistenciaService, AlahiaPos.DataAccess.Servicios.Rrhh.RrhhAsistenciaService>();
builder.Services.AddScoped<INominaProcesoService, AlahiaPos.DataAccess.Servicios.Rrhh.NominaProcesoService>();
builder.Services.AddScoped<IManufacturaService, AlahiaPos.DataAccess.Servicios.Manufactura.ManufacturaService>();
builder.Services.AddScoped<INominaReciboEnvioService, AlahiaPosApi.Servicios.NominaReciboEnvioService>();
builder.Services.AddSingleton<AlahiaPos.Payroll.Abstractions.IPayrollRulePack>(_ => AlahiaPos.Payroll.Rules.DO.DominicanRulePack.Create());
builder.Services.AddPayrollInfrastructure(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
builder.Services.AddSingleton<AlahiaPos.Payroll.Abstractions.IPayrollEngine, AlahiaPos.Payroll.Core.Engine.PayrollEngine>();
builder.Services.AddScoped<IParametrosService, ParametrosService>();
builder.Services.AddScoped<IPlantillasDocumentosClinicos, PlantillasDocumentosClinicosService>();
builder.Services.AddScoped<IDocumentosClinicos, DocumentosClinicosService>();
builder.Services.AddScoped<IFichaClinica, FichaClinicaService>();
builder.Services.AddScoped<IHistorialServicios, HistorialServiciosService>();
builder.Services.AddScoped<IPrinterTicket, PrinterTicketServices>();



builder.Services.AddScoped<IPagoEmpresaService, PagoEmpresaService>();
builder.Services.AddScoped<IVoucherMontoReader, AlahiaPos.DataAccess.Servicios.Suscripciones.VoucherMontoReader>();

builder.Services.AddSignalR();
builder.Services.AddScoped<ITicketsService, TicketsService>();

// =============================
// Cotizador comercial (web pública)
// =============================
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.ICotizadorRecomendador,
    AlahiaPos.DataAccess.Servicios.Cotizador.ReglasRecomendador>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.ICotizadorService,
    AlahiaPos.DataAccess.Servicios.Cotizador.CotizadorService>();

// =============================
// Alahia AI (multi-provider)
// =============================
builder.Services.Configure<AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiOptions>(
    builder.Configuration.GetSection(AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiOptions.SectionName));
builder.Services.AddHttpClient("AlahiaAi");
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProvider>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("AlahiaAi");
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiOptions>>();
    return new AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.OpenAiProvider(http, opts);
});
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProvider>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("AlahiaAi");
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiOptions>>();
    return new AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.AzureOpenAiProvider(http, opts);
});
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProvider>(sp =>
{
    var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("AlahiaAi");
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiOptions>>();
    return new AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.OllamaProvider(http, opts);
});
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProvider, AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.AnthropicProvider>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProvider, AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.GeminiProvider>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiProviderFactory, AlahiaPos.DataAccess.Servicios.AlahiaAi.Providers.AiProviderFactory>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiPromptManager, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiPromptManager>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiContextBuilder, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiContextBuilder>();
builder.Services.AddSingleton<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiConversationHistory, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiConversationHistoryService>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiPermissionService, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiPermissionService>();
builder.Services.AddSingleton<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiUsageMonitor, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiUsageMonitor>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAlahiaAiErpGateway, AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiErpGateway>();
builder.Services.AddScoped<AlahiaPos.DataAccess.Servicios.AlahiaAi.IEmpresaAiConfigService, AlahiaPos.DataAccess.Servicios.AlahiaAi.EmpresaAiConfigService>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAiSqlExecutor, AlahiaPos.DataAccess.Servicios.AlahiaAi.AiSqlExecutor>();
builder.Services.AddScoped<AlahiaPos.Entities.Interfaces.AlahiaAi.IAlahiaAiService, AlahiaPos.DataAccess.Servicios.AlahiaAi.AlahiaAiService>();

builder.Services.AddScoped<INotificacionCentro, AlahiaPos.DataAccess.Servicios.Notificaciones.NotificacionCentroService>();
builder.Services.AddScoped<INotificacionCanal, AlahiaPos.DataAccess.Servicios.Notificaciones.EmailNotificacionCanal>();
builder.Services.AddScoped<INotificacionCanal, AlahiaPos.DataAccess.Servicios.Notificaciones.PushNotificacionCanalStub>();
builder.Services.AddScoped<INotificacionCanal, AlahiaPos.DataAccess.Servicios.Notificaciones.WhatsAppNotificacionCanalStub>();
builder.Services.AddSingleton<INotificacionRealtime, AlahiaPosApi.Hubs.SignalRNotificacionRealtime>();
builder.Services.AddScoped<INotificacionSuscripcionCanal, AlahiaPos.DataAccess.Servicios.Suscripciones.EmailSuscripcionCanal>();
builder.Services.AddScoped<INotificacionSuscripcionCanal, AlahiaPos.DataAccess.Servicios.Suscripciones.InAppSuscripcionCanal>();
builder.Services.AddScoped<INotificacionSuscripcionCanal, AlahiaPos.DataAccess.Servicios.Suscripciones.WhatsAppSuscripcionCanalStub>();
builder.Services.AddScoped<ISuscripcionCobroService, AlahiaPos.DataAccess.Servicios.Suscripciones.SuscripcionCobroService>();
builder.Services.AddScoped<IEmpresaCargoRecurrenteService, AlahiaPos.DataAccess.Servicios.Suscripciones.EmpresaCargoRecurrenteService>();
builder.Services.AddHostedService<AlahiaPosApi.Workers.SuscripcionBillingWorker>();
builder.Services.AddScoped<IBizcochoEncargoService, BizcochoEncargoServices>();
builder.Services.AddScoped<ILavadorConsumoServices, LavadorConsumoServices>();
builder.Services.Configure<DgiiSettings>(builder.Configuration.GetSection("DGII"));


builder.Services.Configure<WhatsAppCitasOptions>(
    builder.Configuration.GetSection(WhatsAppCitasOptions.Section));
builder.Services.AddScoped<IWhatsAppCitas, AlahiaPos.DataAccess.Servicios.WhatsApp.WhatsAppCitasService>();
builder.Services.AddHostedService<AlahiaPosApi.Workers.CitasWhatsAppRecordatorioWorker>();

// =============================
// ? DGII / E-CF (lo nuevo)
// =============================


builder.Services.Configure<DgiiSettings>(
    builder.Configuration.GetSection("DGII"));

builder.Services.AddHttpClient();

// =============================
// AUTH COOKIE CROSS DOMAIN
// =============================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
.AddCookie(options =>
{
    options.Cookie.Name = "AlahiaAuth";
    options.Cookie.Domain = ".alahiapos.com";
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
});

// =============================
// CORS TOTALMENTE ABIERTO
// =============================
builder.Services.AddCors(options =>
{
    options.AddPolicy("PolicyConfig", policy =>
    {
        policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// =============================
// AutoMapper
// =============================
builder.Services.AddAutoMapper(typeof(Program).Assembly);
builder.Services.AddAutoMapper(cfg => cfg.DisableConstructorMapping());

// =============================
// BUILD
// =============================
var app = builder.Build();

// =============================
// DEV
// =============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =============================
// PIPELINE
// =============================
//app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("PolicyConfig");

// Archivos estáticos de updates (instalador Printer Agent, latest.json, etc.)
// Carpeta física: {ContentRoot}/updates  →  URL: /updates/...
// En IIS demo: C:\inetpub\wwwroot\AlahiaPosApi_Demo\updates\printer\...
var updatesPath = Path.Combine(app.Environment.ContentRootPath, "updates");
Directory.CreateDirectory(updatesPath);
var contentTypes = new FileExtensionContentTypeProvider();
contentTypes.Mappings[".zip"] = "application/zip";
contentTypes.Mappings[".json"] = "application/json";
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(updatesPath),
    RequestPath = "/updates",
    ContentTypeProvider = contentTypes,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AlahiaPosApi.Hubs.NotificacionesHub>("/hubs/notificaciones");
app.MapHub<AlahiaPosApi.Hubs.ProduccionHub>("/hubs/produccion");

app.Run();

public partial class Program { }