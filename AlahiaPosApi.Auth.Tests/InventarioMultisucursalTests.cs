using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Repository;
using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;
using Xunit;

namespace AlahiaPosApi.Auth.Tests
{
    [Collection("InventarioMultisucursal")]
    public class InventarioMultisucursalTests : IAsyncLifetime
    {
        private AlahiaPosContext _ctx = null!;
        private IDbContextTransaction _tx = null!;
        private IAlmacenes _almacenes = null!;
        private IAlmacenExistencia _existencias = null!;
        private ISucursalService _sucursales = null!;
        private IMovimientosInventarioService _movimientos = null!;
        private IProductos _productosSvc = null!;

        private int _idEmpresa;
        private int _idEmpresaOtra;
        private int _idSucursalA;
        private int _idSucursalB;
        private int _idSucursalOtra;
        private int _idAlmacenA;
        private int _idAlmacenB;
        private int _idAlmacenOtra;
        private int _idProducto;
        private int _idProductoOtra;
        private int _idUsuarioA;
        private int _idUsuarioB;
        private int _idUsuarioAmbas;

        public async Task InitializeAsync()
        {
            _ctx = CreateContext();
            await AsegurarIndiceUnicoExistenciasAsync();
            _tx = await _ctx.Database.BeginTransactionAsync();

            var repoAlmacen = new BaseRepository<Almacen>(_ctx);
            _almacenes = new AlmacenesServices(repoAlmacen);
            _existencias = new AlmacenExistenciaServices(
                new BaseRepository<AlmacenExistencia>(_ctx),
                repoAlmacen,
                new BaseRepository<Productos>(_ctx));
            _sucursales = new SucursalService(_ctx);
            _movimientos = new MovimientosInventarioServices(
                new BaseRepository<MovimientosInventario>(_ctx),
                new BaseRepository<MovimientosInventarioDetalle>(_ctx),
                new BaseRepository<Productos>(_ctx),
                _existencias,
                _almacenes,
                _sucursales,
                new BaseRepository<Usuarios>(_ctx),
                new NoopContabilidad());
            _productosSvc = new IProductosServices(
                new BaseRepository<Productos>(_ctx),
                _ctx);

            await SeedAsync();
        }

        public async Task DisposeAsync()
        {
            if (_tx != null)
                await _tx.RollbackAsync();
            if (_ctx != null)
                await _ctx.DisposeAsync();
        }

        [Fact]
        public async Task Usuario_sucursal_A_no_consulta_ni_modifica_inventario_de_B()
        {
            var almacenesA = (await _almacenes.GetAllAlmacenes(_idEmpresa, _idSucursalA)).ToList();
            Assert.DoesNotContain(almacenesA, x => x.IdAlmacen == _idAlmacenB);

            var detalleA = await _existencias.GetDetallePorProducto(
                _idProducto, _idEmpresa, _idSucursalA);
            Assert.DoesNotContain(detalleA, x => x.IdAlmacen == _idAlmacenB);

            var exOrigen = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _movimientos.GuardarMovimiento(SalidaVenta(
                    _idEmpresa, _idSucursalA, _idAlmacenB, _idUsuarioA, 1)));
            Assert.Contains("sucursal", exOrigen.Message, StringComparison.OrdinalIgnoreCase);

            var exAcceso = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _movimientos.GuardarMovimiento(SalidaVenta(
                    _idEmpresa, _idSucursalB, _idAlmacenB, _idUsuarioA, 1)));
            Assert.Contains("no tiene acceso", exAcceso.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Venta_en_A_descuenta_almacen_A_y_no_afecta_B()
        {
            var aAntes = await CantidadAsync(_idAlmacenA);
            var bAntes = await CantidadAsync(_idAlmacenB);

            await _movimientos.GuardarMovimiento(SalidaVenta(
                _idEmpresa, _idSucursalA, _idAlmacenA, _idUsuarioA, 7));

            Assert.Equal(aAntes - 7, await CantidadAsync(_idAlmacenA));
            Assert.Equal(bAntes, await CantidadAsync(_idAlmacenB));
        }

        [Fact]
        public async Task Venta_en_B_no_afecta_inventario_de_A()
        {
            var aAntes = await CantidadAsync(_idAlmacenA);
            var bAntes = await CantidadAsync(_idAlmacenB);

            await _movimientos.GuardarMovimiento(SalidaVenta(
                _idEmpresa, _idSucursalB, _idAlmacenB, _idUsuarioB, 3));

            Assert.Equal(aAntes, await CantidadAsync(_idAlmacenA));
            Assert.Equal(bAntes - 3, await CantidadAsync(_idAlmacenB));
        }

        [Fact]
        public async Task Transferencia_A_hacia_B_descuenta_A_y_aumenta_B()
        {
            var aAntes = await CantidadAsync(_idAlmacenA);
            var bAntes = await CantidadAsync(_idAlmacenB);

            await _movimientos.GuardarMovimiento(new MovimientosInventario
            {
                TipoMovimiento = "TRANSFERENCIA",
                Motivo = "TRANSFERENCIA",
                IdEmpresa = _idEmpresa,
                IdSucursal = _idSucursalA,
                IdAlmacen = _idAlmacenA,
                IdAlmacenDestino = _idAlmacenB,
                IdUsuario = _idUsuarioAmbas,
                Fecha = DateTime.Now,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = _idProducto, Cantidad = 11, Precio = 0 }
                }
            });

            Assert.Equal(aAntes - 11, await CantidadAsync(_idAlmacenA));
            Assert.Equal(bAntes + 11, await CantidadAsync(_idAlmacenB));
        }

        [Fact]
        public async Task No_se_puede_transferir_entre_empresas_diferentes()
        {
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _movimientos.GuardarMovimiento(new MovimientosInventario
                {
                    TipoMovimiento = "TRANSFERENCIA",
                    Motivo = "TRANSFERENCIA",
                    IdEmpresa = _idEmpresa,
                    IdSucursal = _idSucursalA,
                    IdAlmacen = _idAlmacenA,
                    IdAlmacenDestino = _idAlmacenOtra,
                    IdUsuario = _idUsuarioAmbas,
                    Fecha = DateTime.Now,
                    Activo = true,
                    Detalles = new List<MovimientosInventarioDetalle>
                    {
                        new() { IdProducto = _idProducto, Cantidad = 1, Precio = 0 }
                    }
                }));

            Assert.Contains("empresas diferentes", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(100, await CantidadAsync(_idAlmacenA));
        }

        [Fact]
        public async Task Usuario_con_varias_sucursales_cambia_y_opera_la_seleccionada()
        {
            var cambio = await _sucursales.CambiarActivaAsync(
                _idUsuarioAmbas, _idEmpresa, _idSucursalB, "test", "127.0.0.1");
            Assert.Equal(_idSucursalB, cambio.IdSucursal);

            var bAntes = await CantidadAsync(_idAlmacenB);
            var aAntes = await CantidadAsync(_idAlmacenA);

            await _movimientos.GuardarMovimiento(SalidaVenta(
                _idEmpresa, _idSucursalB, _idAlmacenB, _idUsuarioAmbas, 4));

            Assert.Equal(bAntes - 4, await CantidadAsync(_idAlmacenB));
            Assert.Equal(aAntes, await CantidadAsync(_idAlmacenA));

            var ventaA = await _productosSvc.GetAllProductosVenta(_idEmpresa, _idSucursalA);
            var ventaB = await _productosSvc.GetAllProductosVenta(_idEmpresa, _idSucursalB);
            var prodA = Assert.Single(ventaA, p => p.IdProducto == _idProducto);
            var prodB = Assert.Single(ventaB, p => p.IdProducto == _idProducto);
            Assert.Equal(aAntes, prodA.Cantidad);
            Assert.Equal(bAntes - 4, prodB.Cantidad);
        }

        [Fact]
        public async Task Empresa_con_una_sola_sucursal_mantiene_comportamiento_actual()
        {
            var todos = (await _almacenes.GetAllAlmacenes(_idEmpresaOtra)).ToList();
            var deSucursal = (await _almacenes.GetAllAlmacenes(
                _idEmpresaOtra, _idSucursalOtra)).ToList();
            Assert.Equal(todos.Select(x => x.IdAlmacen).OrderBy(x => x),
                deSucursal.Select(x => x.IdAlmacen).OrderBy(x => x));

            var principalEmpresa = await _almacenes.GetAlmacenPrincipal(_idEmpresaOtra);
            var principalSucursal = await _almacenes.GetAlmacenPrincipal(
                _idEmpresaOtra, _idSucursalOtra);
            Assert.NotNull(principalEmpresa);
            Assert.Equal(principalEmpresa!.IdAlmacen, principalSucursal!.IdAlmacen);
            Assert.Equal(_idAlmacenOtra, principalEmpresa.IdAlmacen);

            var antes = await CantidadAsync(_idAlmacenOtra, _idProductoOtra);
            await _movimientos.GuardarMovimiento(new MovimientosInventario
            {
                TipoMovimiento = "SALIDA",
                Motivo = "VENTA",
                IdEmpresa = _idEmpresaOtra,
                IdSucursal = _idSucursalOtra,
                IdAlmacen = _idAlmacenOtra,
                Fecha = DateTime.Now,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = _idProductoOtra, Cantidad = 2, Precio = 0 }
                }
            });
            Assert.Equal(antes - 2, await CantidadAsync(_idAlmacenOtra, _idProductoOtra));
        }

        [Fact]
        public async Task No_se_generan_existencias_duplicadas()
        {
            await _existencias.AjustarExistencia(_idAlmacenA, _idProducto, _idEmpresa, 5);
            await _existencias.AjustarExistencia(_idAlmacenA, _idProducto, _idEmpresa, 3);

            var filas = await _ctx.AlmacenExistencia
                .AsNoTracking()
                .CountAsync(x => x.IdAlmacen == _idAlmacenA && x.IdProducto == _idProducto);
            Assert.Equal(1, filas);
            Assert.Equal(108, await CantidadAsync(_idAlmacenA));
        }

        [Fact]
        public async Task Cantidades_consistentes_despues_de_transferencia_y_ajuste()
        {
            await _movimientos.GuardarMovimiento(new MovimientosInventario
            {
                TipoMovimiento = "TRANSFERENCIA",
                Motivo = "TRANSFERENCIA",
                IdEmpresa = _idEmpresa,
                IdSucursal = _idSucursalA,
                IdAlmacen = _idAlmacenA,
                IdAlmacenDestino = _idAlmacenB,
                IdUsuario = _idUsuarioAmbas,
                Fecha = DateTime.Now,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = _idProducto, Cantidad = 8, Precio = 0 }
                }
            });

            await _movimientos.GuardarMovimiento(new MovimientosInventario
            {
                TipoMovimiento = "ENTRADA",
                Motivo = "AJUSTE",
                IdEmpresa = _idEmpresa,
                IdSucursal = _idSucursalA,
                IdAlmacen = _idAlmacenA,
                IdUsuario = _idUsuarioA,
                Fecha = DateTime.Now,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = _idProducto, Cantidad = 2, Precio = 0 }
                }
            });

            Assert.Equal(94, await CantidadAsync(_idAlmacenA));
            Assert.Equal(28, await CantidadAsync(_idAlmacenB));
            Assert.Equal(94, await _existencias.GetTotalPorProducto(
                _idProducto, _idEmpresa, _idSucursalA));
            Assert.Equal(28, await _existencias.GetTotalPorProducto(
                _idProducto, _idEmpresa, _idSucursalB));
            Assert.Equal(122, await _existencias.GetTotalPorProducto(
                _idProducto, _idEmpresa));
        }

        private MovimientosInventario SalidaVenta(
            int idEmpresa,
            int idSucursal,
            int idAlmacen,
            int idUsuario,
            decimal cantidad)
        {
            return new MovimientosInventario
            {
                TipoMovimiento = "SALIDA",
                Motivo = "VENTA",
                IdEmpresa = idEmpresa,
                IdSucursal = idSucursal,
                IdAlmacen = idAlmacen,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Activo = true,
                Detalles = new List<MovimientosInventarioDetalle>
                {
                    new() { IdProducto = _idProducto, Cantidad = cantidad, Precio = 0 }
                }
            };
        }

        private async Task<decimal> CantidadAsync(int idAlmacen, int? idProducto = null)
        {
            var producto = idProducto ?? _idProducto;
            return await _ctx.AlmacenExistencia
                .AsNoTracking()
                .Where(x => x.IdAlmacen == idAlmacen && x.IdProducto == producto)
                .Select(x => x.Cantidad)
                .SumAsync();
        }

        private async Task AsegurarIndiceUnicoExistenciasAsync()
        {
            await _ctx.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.AlmacenExistencias', N'U') IS NOT NULL
BEGIN
    ;WITH agrupado AS
    (
        SELECT
            IdAlmacen,
            IdProducto,
            MIN(IdAlmacenExistencia) AS IdKeep,
            SUM(Existencia) AS TotalExistencia
        FROM dbo.AlmacenExistencias
        GROUP BY IdAlmacen, IdProducto
        HAVING COUNT(*) > 1
    )
    UPDATE e
    SET e.Existencia = a.TotalExistencia
    FROM dbo.AlmacenExistencias e
    INNER JOIN agrupado a
        ON e.IdAlmacenExistencia = a.IdKeep;

    ;WITH dups AS
    (
        SELECT
            IdAlmacenExistencia,
            ROW_NUMBER() OVER (
                PARTITION BY IdAlmacen, IdProducto
                ORDER BY IdAlmacenExistencia
            ) AS rn
        FROM dbo.AlmacenExistencias
    )
    DELETE FROM dups WHERE rn > 1;
END");

            await _ctx.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.AlmacenExistencias', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_AlmacenExistencias_AlmacenProducto'
          AND object_id = OBJECT_ID(N'dbo.AlmacenExistencias')
   )
BEGIN
    CREATE UNIQUE INDEX UX_AlmacenExistencias_AlmacenProducto
        ON dbo.AlmacenExistencias (IdAlmacen, IdProducto);
END");
        }

        private async Task SeedAsync()
        {
            var marca = Guid.NewGuid().ToString("N")[..8];

            var empresa = await CrearEmpresaAsync($"QA-INV-MS-{marca}");
            _idEmpresa = empresa.IdEmpresa;

            var principal = await _sucursales.AsegurarPrincipalAsync(_idEmpresa);
            _idSucursalA = principal.IdSucursal;
            _idAlmacenA = principal.IdAlmacenPrincipal
                ?? throw new InvalidOperationException("Falta almacén principal A.");

            var sucB = new Sucursal
            {
                IdEmpresa = _idEmpresa,
                Codigo = "SUCB",
                Nombre = "Sucursal B",
                EsPrincipal = false,
                Activa = true,
                FechaCreacion = DateTime.Now
            };
            _ctx.Sucursales.Add(sucB);
            await _ctx.SaveChangesAsync();
            _idSucursalB = sucB.IdSucursal;

            var almacenB = new Almacen
            {
                Nombre = "Almacen B",
                IdEmpresa = _idEmpresa,
                IdSucursal = _idSucursalB,
                EsPrincipal = true,
                Activo = true,
                FechaCreacion = DateTime.Now
            };
            await _almacenes.InsertAlmacen(almacenB);
            sucB.IdAlmacenPrincipal = almacenB.IdAlmacen;
            await _ctx.SaveChangesAsync();
            _idAlmacenB = almacenB.IdAlmacen;

            var producto = new Productos
            {
                IdEmpresa = _idEmpresa,
                Nombre = "QA Inv Prod",
                Descripcion = "QA Inv Prod",
                ControlarStock = true,
                IsActivo = true,
                SeVende = true,
                TipoOperacion = "VENTA",
                TipoComportamiento = "Inventario",
                FechaInseccion = DateTime.Now,
                FechaVencimiento = ""
            };
            _ctx.Productos.Add(producto);
            await _ctx.SaveChangesAsync();
            _idProducto = producto.IdProducto;

            await _existencias.AjustarExistencia(_idAlmacenA, _idProducto, _idEmpresa, 100);
            await _existencias.AjustarExistencia(_idAlmacenB, _idProducto, _idEmpresa, 20);

            var perfil = new Perfiles
            {
                IdEmpresa = _idEmpresa,
                Nombre = "QA Inv",
                Activo = true
            };
            _ctx.Perfiles.Add(perfil);
            var empA = new Empleados { IdEmpresa = _idEmpresa, Nombre = "User A", Estado = true };
            var empB = new Empleados { IdEmpresa = _idEmpresa, Nombre = "User B", Estado = true };
            var empAmbas = new Empleados { IdEmpresa = _idEmpresa, Nombre = "User Ambas", Estado = true };
            _ctx.EmpleadosP.AddRange(empA, empB, empAmbas);
            await _ctx.SaveChangesAsync();

            var userA = CrearUsuario(_idEmpresa, empA.IdEmpleados, perfil.IdPerfil, $"qa.a.{marca}");
            var userB = CrearUsuario(_idEmpresa, empB.IdEmpleados, perfil.IdPerfil, $"qa.b.{marca}");
            var userAmbas = CrearUsuario(_idEmpresa, empAmbas.IdEmpleados, perfil.IdPerfil, $"qa.ab.{marca}");
            _ctx.Usuarios.AddRange(userA, userB, userAmbas);
            await _ctx.SaveChangesAsync();
            _idUsuarioA = userA.IdUsuario;
            _idUsuarioB = userB.IdUsuario;
            _idUsuarioAmbas = userAmbas.IdUsuario;

            await _sucursales.AsegurarAccesoUsuarioAsync(_idUsuarioA, _idEmpresa);
            await _sucursales.AsegurarAccesoUsuarioAsync(_idUsuarioAmbas, _idEmpresa);

            _ctx.UsuarioSucursales.AddRange(
                new UsuarioSucursal
                {
                    IdUsuario = _idUsuarioB,
                    IdSucursal = _idSucursalB,
                    EsDefault = true,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                },
                new UsuarioSucursal
                {
                    IdUsuario = _idUsuarioAmbas,
                    IdSucursal = _idSucursalB,
                    EsDefault = false,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                });
            userB.IdSucursalActiva = _idSucursalB;
            await _ctx.SaveChangesAsync();

            var empresaOtra = await CrearEmpresaAsync($"QA-INV-MS-OTRA-{marca}");
            _idEmpresaOtra = empresaOtra.IdEmpresa;
            var princOtra = await _sucursales.AsegurarPrincipalAsync(_idEmpresaOtra);
            _idSucursalOtra = princOtra.IdSucursal;
            _idAlmacenOtra = princOtra.IdAlmacenPrincipal
                ?? throw new InvalidOperationException("Falta almacén de la otra empresa.");

            var productoOtra = new Productos
            {
                IdEmpresa = _idEmpresaOtra,
                Nombre = "QA Inv Prod Otra",
                Descripcion = "QA Inv Prod Otra",
                ControlarStock = true,
                IsActivo = true,
                SeVende = true,
                TipoOperacion = "VENTA",
                TipoComportamiento = "Inventario",
                FechaInseccion = DateTime.Now,
                FechaVencimiento = ""
            };
            _ctx.Productos.Add(productoOtra);
            await _ctx.SaveChangesAsync();
            _idProductoOtra = productoOtra.IdProducto;
            await _existencias.AjustarExistencia(
                _idAlmacenOtra, _idProductoOtra, _idEmpresaOtra, 15);
        }

        private async Task<Empresas> CrearEmpresaAsync(string nombre)
        {
            var empresa = new Empresas
            {
                NombreComercial = nombre,
                Estado = true,
                EstadoServicio = "ACTIVA",
                NivelSoporte = "STANDARD",
                PoliticasAceptadas = true,
                FechaTerminacion = DateTime.Now.AddYears(1),
                FechaInseccion = DateTime.Now,
                GuidPublico = Guid.NewGuid(),
                LimiteTerminalesPos = 1,
                UsaSSL = false,
                PuertoSMTP = 0,
                PagadoServicio = false,
                MontoServicio = 0,
                CargoAdicional = 0,
                CargoReconexionDop = 0,
                ReconexionPendiente = false,
                TrabajaDomingo = true,
                EsEmpresaSistema = false,
                EsEmisorElectronico = false
            };
            _ctx.Empresas.Add(empresa);
            await _ctx.SaveChangesAsync();
            return empresa;
        }

        private static Usuarios CrearUsuario(int idEmpresa, int idEmpleado, int idPerfil, string userName)
        {
            return new Usuarios
            {
                IdEmpresa = idEmpresa,
                IdEmpleado = idEmpleado,
                IdPerfil = idPerfil,
                UserName = userName,
                PasswordHash = "qa-inv",
                Estado = true,
                FechaCreacion = DateTime.Now
            };
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

            throw new InvalidOperationException(
                "No se encontró connection string de AlahiaPos_Dev.");
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
