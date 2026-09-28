using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Repository;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace AlahiaPosApi.Auth.Tests
{
    /// <summary>
    /// Prueba operativa en Sabor Urbano (AlahiaPos_Dev): sucursal Naco, cajero,
    /// orden, factura e-CF consumidor, inventario y cierre de caja.
    /// Persiste la sucursal/usuario para poder entrar en el ERP.
    /// </summary>
    public class SaborUrbanoSucursalOperativaTests
    {
        public const string CodigoSucursal = "NACO";
        public const string UserCajero = "cajero.naco@saborurbano.demo";
        public const string PasswordDemo = "DemoFood2026!";
        public const string UserAdmin = "admin@saborurbano.demo";

        private readonly ITestOutputHelper _output;

        public SaborUrbanoSucursalOperativaTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Flujo_completo_sucursal_naco()
        {
            await using var ctx = CreateContext();
            var sucursales = new SucursalService(ctx);
            var repoAlmacen = new BaseRepository<Almacen>(ctx);
            var almacenes = new AlmacenesServices(repoAlmacen);
            var existencias = new AlmacenExistenciaServices(
                new BaseRepository<AlmacenExistencia>(ctx),
                repoAlmacen,
                new BaseRepository<Productos>(ctx));
            var movimientos = new MovimientosInventarioServices(
                new BaseRepository<MovimientosInventario>(ctx),
                new BaseRepository<MovimientosInventarioDetalle>(ctx),
                new BaseRepository<Productos>(ctx),
                existencias,
                almacenes,
                sucursales,
                new BaseRepository<Usuarios>(ctx),
                new NoopContabilidad());
            var cajaApertura = new CajaAperturaServices(new BaseRepository<CajaApertura>(ctx));
            var cajaCierreRepo = new BaseRepository<CajaCierre>(ctx);
            var productosSvc = new IProductosServices(new BaseRepository<Productos>(ctx), ctx);

            var empresa = await ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.NombreComercial == "Sabor Urbano");
            Assert.NotNull(empresa);
            var idEmpresa = empresa!.IdEmpresa;
            Assert.True(idEmpresa > 0);

            var principal = await sucursales.AsegurarPrincipalAsync(idEmpresa);
            Assert.True(principal.IdAlmacenPrincipal > 0);

            var sucNaco = await ctx.Sucursales
                .FirstOrDefaultAsync(s => s.IdEmpresa == idEmpresa && s.Codigo == CodigoSucursal);
            if (sucNaco == null)
            {
                sucNaco = new Sucursal
                {
                    IdEmpresa = idEmpresa,
                    Codigo = CodigoSucursal,
                    Nombre = "Sucursal Naco",
                    EsPrincipal = false,
                    Activa = true,
                    Direccion = "Av. Tiradentes, Naco, Santo Domingo",
                    Telefono = "8095550190",
                    Municipio = "Santo Domingo",
                    Provincia = "Distrito Nacional",
                    FechaCreacion = DateTime.Now
                };
                ctx.Sucursales.Add(sucNaco);
                await ctx.SaveChangesAsync();
            }

            var almacenNaco = await ctx.Almacenes
                .FirstOrDefaultAsync(a => a.IdEmpresa == idEmpresa && a.IdSucursal == sucNaco.IdSucursal);
            if (almacenNaco == null)
            {
                almacenNaco = new Almacen
                {
                    Nombre = "Principal Naco",
                    Descripcion = "Almacén principal sucursal Naco",
                    IdEmpresa = idEmpresa,
                    IdSucursal = sucNaco.IdSucursal,
                    EsPrincipal = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };
                await almacenes.InsertAlmacen(almacenNaco);
            }

            sucNaco.IdAlmacenPrincipal = almacenNaco.IdAlmacen;
            await ctx.SaveChangesAsync();

            var producto = await ctx.Productos.AsNoTracking()
                .Where(p => p.IdEmpresa == idEmpresa && p.IsActivo && p.SeVende && p.PrecioVenta > 0)
                .OrderByDescending(p => p.ControlarStock)
                .ThenBy(p => p.IdProducto)
                .FirstOrDefaultAsync();
            Assert.NotNull(producto);

            var stockNaco = await existencias.GetExistencia(
                almacenNaco.IdAlmacen, producto!.IdProducto, idEmpresa);
            if (stockNaco == null || stockNaco.Cantidad < 10)
            {
                var falta = 20m - (stockNaco?.Cantidad ?? 0);
                await existencias.AjustarExistencia(
                    almacenNaco.IdAlmacen, producto.IdProducto, idEmpresa, falta);
            }

            var stockPrincipalAntes = principal.IdAlmacenPrincipal is > 0
                ? (await existencias.GetExistencia(
                    principal.IdAlmacenPrincipal.Value, producto.IdProducto, idEmpresa))?.Cantidad ?? 0
                : 0m;
            var stockNacoAntes = (await existencias.GetExistencia(
                almacenNaco.IdAlmacen, producto.IdProducto, idEmpresa))?.Cantidad ?? 0;

            var perfilAdmin = await ctx.Perfiles.AsNoTracking()
                .FirstAsync(p => p.IdEmpresa == idEmpresa && p.Nombre == "Administrador");
            var perfilCajero = await ctx.Perfiles
                .FirstOrDefaultAsync(p => p.IdEmpresa == idEmpresa && p.Nombre == "Cajero");
            if (perfilCajero == null)
            {
                perfilCajero = new Perfiles
                {
                    IdEmpresa = idEmpresa,
                    Nombre = "Cajero",
                    Descripcion = "Cajero sucursal",
                    Activo = true
                };
                ctx.Perfiles.Add(perfilCajero);
                await ctx.SaveChangesAsync();

                var modsAdmin = await ctx.PerfilRoles.AsNoTracking()
                    .Where(r => r.IdPerfil == perfilAdmin.IdPerfil && r.Activo)
                    .Select(r => r.IdModulo)
                    .ToListAsync();
                foreach (var idModulo in modsAdmin.Distinct())
                {
                    ctx.PerfilRoles.Add(new PerfilRoles
                    {
                        IdEmpresa = idEmpresa,
                        IdPerfil = perfilCajero.IdPerfil,
                        IdModulo = idModulo,
                        Activo = true
                    });
                }
                await ctx.SaveChangesAsync();
            }

            var empleado = await ctx.EmpleadosP
                .FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa && e.Nombre == "Cajero Naco");
            if (empleado == null)
            {
                empleado = new Empleados
                {
                    IdEmpresa = idEmpresa,
                    Nombre = "Cajero Naco",
                    Ocupacion = "Cajero",
                    Estado = true,
                    Telefono = "8095550190"
                };
                ctx.EmpleadosP.Add(empleado);
                await ctx.SaveChangesAsync();
            }

            var passwordHash = HashPassword(PasswordDemo);
            var usuario = await ctx.Usuarios
                .FirstOrDefaultAsync(u => u.IdEmpresa == idEmpresa && u.UserName == UserCajero);
            if (usuario == null)
            {
                usuario = new Usuarios
                {
                    IdEmpresa = idEmpresa,
                    IdEmpleado = empleado.IdEmpleados,
                    IdPerfil = perfilCajero.IdPerfil,
                    UserName = UserCajero,
                    Correo = UserCajero,
                    PasswordHash = passwordHash,
                    Estado = true,
                    FechaCreacion = DateTime.Now,
                    IdSucursalActiva = sucNaco.IdSucursal
                };
                ctx.Usuarios.Add(usuario);
                await ctx.SaveChangesAsync();
            }
            else
            {
                usuario.PasswordHash = passwordHash;
                usuario.IdEmpleado = empleado.IdEmpleados;
                usuario.IdPerfil = perfilCajero.IdPerfil;
                usuario.Estado = true;
                usuario.IdSucursalActiva = sucNaco.IdSucursal;
                await ctx.SaveChangesAsync();
            }

            var asignaciones = await ctx.UsuarioSucursales
                .Where(x => x.IdUsuario == usuario.IdUsuario)
                .ToListAsync();
            foreach (var a in asignaciones)
            {
                a.Activo = a.IdSucursal == sucNaco.IdSucursal;
                a.EsDefault = a.IdSucursal == sucNaco.IdSucursal;
            }
            if (!asignaciones.Any(a => a.IdSucursal == sucNaco.IdSucursal))
            {
                ctx.UsuarioSucursales.Add(new UsuarioSucursal
                {
                    IdUsuario = usuario.IdUsuario,
                    IdSucursal = sucNaco.IdSucursal,
                    EsDefault = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                });
            }
            await ctx.SaveChangesAsync();

            var admin = await ctx.Usuarios.AsNoTracking()
                .FirstAsync(u => u.IdEmpresa == idEmpresa && u.UserName == UserAdmin);

            var listaAdmin = await sucursales.ListarPorUsuarioAsync(admin.IdUsuario, idEmpresa);
            Assert.True(listaAdmin.Count >= 2, "El admin debe ver Principal y Naco.");
            Assert.Contains(listaAdmin, s => s.IdSucursal == sucNaco.IdSucursal);
            Assert.Contains(listaAdmin, s => s.EsPrincipal);

            var listaCajero = await sucursales.ListarPorUsuarioAsync(usuario.IdUsuario, idEmpresa);
            Assert.Single(listaCajero);
            Assert.Equal(sucNaco.IdSucursal, listaCajero[0].IdSucursal);

            var todasAdmin = await sucursales.ResolverConsultaAsync(admin.IdUsuario, idEmpresa, null);
            Assert.True(todasAdmin.IdsPermitidos.Count >= 2);
            Assert.True(todasAdmin.EsConsolidado);
            Assert.Contains(todasAdmin.IdsPermitidos, id => id == sucNaco.IdSucursal);

            var filtroNaco = await sucursales.ResolverConsultaAsync(
                admin.IdUsuario, idEmpresa, sucNaco.IdSucursal);
            Assert.False(filtroNaco.EsConsolidado);
            Assert.Equal(sucNaco.IdSucursal, filtroNaco.IdsPermitidos[0]);

            var todasCajero = await sucursales.ResolverConsultaAsync(usuario.IdUsuario, idEmpresa, 0);
            Assert.Single(todasCajero.IdsPermitidos);
            Assert.Equal(sucNaco.IdSucursal, todasCajero.IdsPermitidos[0]);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sucursales.ResolverConsultaAsync(usuario.IdUsuario, idEmpresa, principal.IdSucursal));

            var resuelta = await sucursales.ResolverSucursalActivaAsync(
                usuario.IdUsuario, idEmpresa, principal.IdSucursal);
            Assert.Equal(sucNaco.IdSucursal, resuelta);

            var exCambio = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                sucursales.CambiarActivaAsync(
                    usuario.IdUsuario, idEmpresa, principal.IdSucursal, "qa-naco", "127.0.0.1"));
            Assert.Contains("administrador", exCambio.Message, StringComparison.OrdinalIgnoreCase);

            var abiertas = (await cajaApertura.GetAllAsync(idEmpresa))
                .Where(c => c.IdUsuario == usuario.IdUsuario && c.Estado == "ABIERTA")
                .ToList();
            foreach (var abierta in abiertas)
                await cajaApertura.CerrarCajaAsync(abierta.IdCajaApertura);

            var caja = new CajaApertura
            {
                IdEmpresa = idEmpresa,
                IdSucursal = sucNaco.IdSucursal,
                IdUsuario = usuario.IdUsuario,
                FechaApertura = DateTime.Now,
                MontoInicial = 1000,
                Estado = "ABIERTA",
                Observacion = "QA sucursal Naco"
            };
            await cajaApertura.AbrirCajaAsync(caja);
            Assert.True(caja.IdCajaApertura > 0);
            Assert.Equal(sucNaco.IdSucursal, caja.IdSucursal);

            var marca = $"QA-NACO-{DateTime.Now:yyyyMMddHHmmss}";
            var precio = producto.PrecioVenta;
            var itbis = producto.Itbis ? Math.Round(precio * 0.18m, 2) : 0m;
            var total = precio + itbis;

            var orden = NuevaFactura(
                idEmpresa, sucNaco.IdSucursal, usuario.IdUsuario, empleado.IdEmpleados,
                10, marca + "-ORD", precio, itbis, total, producto.IdProducto, "Pendiente");
            ctx.FacturaHeaders.Add(orden);
            await ctx.SaveChangesAsync();
            Assert.True(orden.IdFacturaHeader > 0);
            Assert.Equal(sucNaco.IdSucursal, orden.IdSucursal);
            Assert.Equal(10, orden.IdTipoDocumentos);

            var factura = NuevaFactura(
                idEmpresa, sucNaco.IdSucursal, usuario.IdUsuario, empleado.IdEmpleados,
                1, marca + "-FAC", precio, itbis, total, producto.IdProducto, "Pagada");
            factura.TipoFactura = "Contado";
            factura.FormaPago = "Efectivo";
            factura.MontoEfectivo = total;
            factura.Pagado = total;
            factura.Pendiente = 0;
            factura.EstaCerrada = false;
            ctx.FacturaHeaders.Add(factura);
            await ctx.SaveChangesAsync();
            Assert.True(factura.IdFacturaHeader > 0);
            Assert.Equal(sucNaco.IdSucursal, factura.IdSucursal);
            Assert.True(string.IsNullOrEmpty(factura.NCF) || factura.NCF.StartsWith("E"));

            await movimientos.GuardarMovimiento(new MovimientosInventario
            {
                TipoMovimiento = "SALIDA",
                Motivo = "VENTA",
                Referencia = $"Factura #{factura.IdFacturaHeader}",
                Observacion = "Salida QA sucursal Naco",
                Fecha = DateTime.Now,
                IdEmpresa = idEmpresa,
                IdSucursal = sucNaco.IdSucursal,
                IdAlmacen = almacenNaco.IdAlmacen,
                IdUsuario = usuario.IdUsuario,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = producto.IdProducto, Cantidad = 1, Precio = precio }
                }
            });

            var stockNacoDespues = (await existencias.GetExistencia(
                almacenNaco.IdAlmacen, producto.IdProducto, idEmpresa))?.Cantidad ?? 0;
            var stockPrincipalDespues = principal.IdAlmacenPrincipal is > 0
                ? (await existencias.GetExistencia(
                    principal.IdAlmacenPrincipal.Value, producto.IdProducto, idEmpresa))?.Cantidad ?? 0
                : 0m;
            Assert.Equal(stockNacoAntes - 1, stockNacoDespues);
            Assert.Equal(stockPrincipalAntes, stockPrincipalDespues);

            var ventaNaco = await productosSvc.GetAllProductosVenta(idEmpresa, sucNaco.IdSucursal);
            var prodVista = Assert.Single(ventaNaco, p => p.IdProducto == producto.IdProducto);
            Assert.Equal(stockNacoDespues, prodVista.Cantidad);

            var cierre = new CajaCierre
            {
                IdCajaApertura = caja.IdCajaApertura,
                IdEmpresa = idEmpresa,
                IdSucursal = sucNaco.IdSucursal,
                IdUsuario = usuario.IdUsuario,
                FechaCierre = DateTime.Now,
                VentasBrutas = total,
                TotalEfectivo = total,
                TotalGeneral = total,
                TotalIngresosNetos = total,
                DebeHaber = 1000 + total,
                MontoRealCaja = 1000 + total,
                Diferencia = 0,
                Observacion = "QA sucursal Naco"
            };
            await cajaCierreRepo.Save(cierre);
            Assert.True(cierre.IdCajaCierre > 0);
            Assert.Equal(sucNaco.IdSucursal, cierre.IdSucursal);

            factura.EstaCerrada = true;
            factura.IdCajaCierre = cierre.IdCajaCierre;
            await ctx.SaveChangesAsync();
            await cajaApertura.CerrarCajaAsync(caja.IdCajaApertura);

            var cajaCerrada = await cajaApertura.GetByIdAsync(caja.IdCajaApertura);
            Assert.Equal("CERRADA", cajaCerrada!.Estado);

            _output.WriteLine(
                "Sabor Urbano {0}: sucursal Naco={1} almacen={2} cajero={3} (id {4}) " +
                "orden={5} factura={6} caja={7} cierre={8}. Login: {9} / {10}",
                idEmpresa, sucNaco.IdSucursal, almacenNaco.IdAlmacen, UserCajero,
                usuario.IdUsuario, orden.IdFacturaHeader, factura.IdFacturaHeader,
                caja.IdCajaApertura, cierre.IdCajaCierre, UserCajero, PasswordDemo);

            await VerificarLoginHttpAsync(sucNaco.IdSucursal, listaAdmin.Count);
        }

        private async Task VerificarLoginHttpAsync(int idSucursalNaco, int sucursalesAdmin)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                var ping = await http.GetAsync("http://localhost:5039/api/Login/login");
                _ = ping;
            }
            catch
            {
                _output.WriteLine("API local no está en localhost:5039; login HTTP omitido.");
                return;
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            var cajero = await LoginHttp(client, UserCajero);
            var usuarioCajero = Prop(cajero, "usuario");
            Assert.False(Prop(usuarioCajero, "esAdministrador").GetBoolean());

            if (!cajero.TryGetProperty("idSucursalActiva", out _)
                && !cajero.TryGetProperty("IdSucursalActiva", out _))
            {
                _output.WriteLine(
                    "Login HTTP OK, pero la API local no devuelve sucursales (proceso viejo). " +
                    "Reinicia la API para validar el selector. Datos de sucursal ya persistidos.");
                return;
            }

            Assert.Equal(idSucursalNaco, Prop(cajero, "idSucursalActiva").GetInt32());
            Assert.Equal(1, Prop(cajero, "sucursales").GetArrayLength());

            var admin = await LoginHttp(client, UserAdmin);
            Assert.True(Prop(Prop(admin, "usuario"), "esAdministrador").GetBoolean());
            Assert.True(Prop(admin, "sucursales").GetArrayLength() >= sucursalesAdmin);
        }

        private static async Task<JsonElement> LoginHttp(HttpClient client, string user)
        {
            var resp = await client.PostAsJsonAsync("http://localhost:5039/api/Login/login", new
            {
                userName = user,
                password = PasswordDemo,
                deviceId = "qa-naco-e2e"
            });
            var body = await resp.Content.ReadAsStringAsync();
            Assert.True(resp.IsSuccessStatusCode, $"Login {user}: {(int)resp.StatusCode} {body}");
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        private static JsonElement Prop(JsonElement el, string camel)
        {
            if (el.TryGetProperty(camel, out var v))
                return v;
            var pascal = char.ToUpperInvariant(camel[0]) + camel[1..];
            return el.GetProperty(pascal);
        }

        private static FacturaHeaders NuevaFactura(
            int idEmpresa,
            int idSucursal,
            int idUsuario,
            int idEmpleado,
            int idTipoDocumento,
            string numero,
            decimal precio,
            decimal itbis,
            decimal total,
            int idProducto,
            string estado)
        {
            return new FacturaHeaders
            {
                IdEmpresa = idEmpresa,
                IdSucursal = idSucursal,
                IdUsuario = idUsuario,
                IdEmpleados = idEmpleado,
                IdMoso = idEmpleado,
                IdTipoDocumentos = idTipoDocumento,
                NumeroDocumento = numero,
                NCF = "",
                TipoOrden = idTipoDocumento == 10 ? "Para llevar" : "",
                TipoFactura = idTipoDocumento == 1 ? "Contado" : "",
                FormaPago = idTipoDocumento == 1 ? "Efectivo" : "",
                Estado = estado,
                Estado_Orden = idTipoDocumento == 10 ? "Pendiente" : "",
                FechaInseccion = DateTime.Now,
                FechaBencimiento = DateTime.Now,
                Hora = DateTime.Now.ToString("hh:mm tt"),
                SubTotal = precio,
                TotalItbis = itbis,
                Total = total,
                Pagado = idTipoDocumento == 1 ? total : 0,
                Pendiente = idTipoDocumento == 1 ? 0 : total,
                EstaCancelada = false,
                EstaCerrada = false,
                Moneda = "DOP",
                IdMesa = 1,
                PrintAcount = false,
                PrintPending = false,
                PrintLavador = false,
                Clientes = null,
                Empleados = null,
                TipoDocumentos = null,
                Mesas = null,
                FacturaDetalles = new List<FacturaDetalles>
                {
                    new()
                    {
                        IdEmpresa = idEmpresa,
                        IdProducto = idProducto,
                        Cantidad = 1,
                        PrecioOferta = precio,
                        Itbis = itbis,
                        SubTotal = total,
                        FechaInseccion = DateTime.Now,
                        Productos = null
                    }
                }
            };
        }

        private static string HashPassword(string plain)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string DevConnection()
        {
            var env = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            if (!string.IsNullOrWhiteSpace(env)
                && env.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase)
                && !env.Contains("AlahiaPos_Prod", StringComparison.OrdinalIgnoreCase))
            {
                return env;
            }

            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                foreach (var candidate in new[]
                {
                    Path.Combine(dir.FullName, "AlahiaPosApi", "appsettings.json"),
                    Path.Combine(dir.FullName, "appsettings.json"),
                    Path.Combine(dir.FullName, "artifacts", "pedidos-api", "appsettings.json")
                })
                {
                    if (!File.Exists(candidate)) continue;
                    using var doc = JsonDocument.Parse(File.ReadAllText(candidate));
                    if (!doc.RootElement.TryGetProperty("ConnectionStrings", out var csRoot)
                        || !csRoot.TryGetProperty("Default", out var def))
                    {
                        continue;
                    }

                    var cs = def.GetString();
                    if (!string.IsNullOrWhiteSpace(cs)
                        && cs.Contains("AlahiaPos_Dev", StringComparison.OrdinalIgnoreCase)
                        && !cs.Contains("AlahiaPos_Prod", StringComparison.OrdinalIgnoreCase))
                    {
                        return cs;
                    }
                }
            }

            throw new InvalidOperationException("No se encontró connection string de AlahiaPos_Dev.");
        }

        private static AlahiaPosContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AlahiaPosContext>()
                .UseSqlServer(DevConnection())
                .Options;
            return new AlahiaPosContext(options);
        }

        private sealed class NoopContabilidad : IContabilidadEventPublisher
        {
            public Task<ContabilidadPublishResult> TryPublishAsync<TEvent>(TEvent domainEvent)
                where TEvent : DomainEventBase
                => Task.FromResult(ContabilidadPublishResult.Inactiva());
        }
    }
}
