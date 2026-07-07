using Alahia_Pos.Services;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Repository;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Setting;
using AlahiaPosApi;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PrinterLibrary;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Controllers + Swagger
// =============================
builder.Services.AddControllers();
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
builder.Services.AddScoped<IGastos, GastosServices>();
builder.Services.AddScoped<IEmpresas, EmpresaServices>();
builder.Services.AddScoped<ICocinas, CocinaServices>();
builder.Services.AddScoped<IParametroConfig, IParametroCOnfigServices>();
builder.Services.AddScoped<IValidateIMpuesto, ValidateImpuestos>();
builder.Services.AddScoped<IAreas, AreaServices>();
builder.Services.AddScoped<IEmpleadoAreaComisionService, EmpleadoAreaComisionService>();
builder.Services.AddScoped<ICitas, CitaServices>();
builder.Services.AddScoped<IHorariosEstilista, HorarioEstilistaServices>();
builder.Services.AddScoped<IRNCService, RNCService>();
builder.Services.AddScoped<IDescuentoHeader, DescuentoHeaderServices>();
builder.Services.AddScoped<IDescuentoDetalle, DescuentoDetalleServices>();
builder.Services.AddScoped<IPagosFacturasClientes, PagosFacturasClientesService>();
builder.Services.AddScoped<IIngresos, IngresosService>();
builder.Services.AddScoped<IDescuentoAreaDetalle, DescuentoAreaDetalleService>();
builder.Services.AddScoped<INCF_Secuencias, NCF_SecuenciasServices>();
builder.Services.AddScoped<INotification, NotificationServices>();
builder.Services.AddScoped<IModulo, ModulosServices>();
builder.Services.AddScoped<IEmpresaModulos, EmpresaModulosServices>();
builder.Services.AddScoped<IPlanesCloud, PlanesCloudService>();
builder.Services.AddScoped<ISecuenciaDocumentoService, SecuenciaDocumentoService>();
builder.Services.AddScoped<ICajaCierreService, CajaCierreServices>();
builder.Services.AddScoped<ICajaAperturaService, CajaAperturaServices>();
builder.Services.AddScoped<ICajaMovimientoService, CajaMovimientoServices>();
builder.Services.AddScoped<ICuentaFinancieraService, CuentaFinancieraService>();
builder.Services.AddScoped<IMovimientoFinancieroService, MovimientoFinancieroService>();
builder.Services.AddScoped<IMetodoPagoCuentaService, MetodoPagoCuentaService>();
// ======================================================
// 🔥 MOVIMIENTOS INVENTARIO
// ======================================================

builder.Services.AddScoped<
    IMovimientosInventarioService,
    MovimientosInventarioServices>();
builder.Services.AddScoped<IUsuarios, UsuariosService>();
builder.Services.AddScoped<IPerfiles, PerfilesService>();
builder.Services.AddScoped<IPerfilRoles, PerfilRolesService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<IEmpleados, EmpleadosService>();
builder.Services.AddScoped<IParametrosService, ParametrosService>();
builder.Services.AddScoped<IPrinterTicket, PrinterTicketServices>();



builder.Services.AddScoped<IPagoEmpresaService, PagoEmpresaService>();
builder.Services.AddScoped<IBizcochoEncargoService, BizcochoEncargoServices>();
builder.Services.AddScoped<ILavadorConsumoServices, LavadorConsumoServices>();
builder.Services.Configure<DgiiSettings>(builder.Configuration.GetSection("DGII"));


builder.Services.AddScoped<TwilioService>();

// =============================
// ✅ DGII / E-CF (lo nuevo)
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();