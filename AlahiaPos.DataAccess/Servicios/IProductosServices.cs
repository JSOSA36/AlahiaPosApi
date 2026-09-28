using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IProductosServices : IProductos
    {
        IRepository<Productos> Repository;
        private readonly AlahiaPosContext _context;

        public IProductosServices(IRepository<Productos> repository, AlahiaPosContext context)
        {
            Repository = repository;
            _context = context;
        }
        // 🚀 Nuevo método: traer solo servicios por área
        public async Task<IEnumerable<ProductoLiteDto>> GetLite(int IdEmpresa)
        {
            var prods = await Repository.GetAllByExpresionAsync(p => p.IdEmpresa == IdEmpresa);

            return prods.Select(p => new ProductoLiteDto
            {
                IdProducto = p.IdProducto,
                Descripcion = p.Descripcion
            });
        }
        public async Task<IEnumerable<Productos>> GetServiciosByArea(int idArea, int idEmpresa)
        {
            return await Repository.GetAllByExpresionAsync(c =>
                c.IdArea == idArea &&
                c.IdEmpresa == idEmpresa &&
                c.EsServicio == true &&
                c.IsActivo &&
                c.SeVende
            );
        }

        public Productos GetProductoById(int Id)
        {
            return Repository.GetById(Id);
        }

        /// <returns>true si se borró la fila; false si quedó inactivo por tener movimiento.</returns>
        public bool DeleteProductos(int IdProductos)
        {
            var producto = Repository.GetById(IdProductos)
                ?? throw new InvalidOperationException("Producto no encontrado");

            if (TieneMovimiento(IdProductos))
            {
                if (producto.IsActivo)
                {
                    producto.IsActivo = false;
                    Repository.Update(IdProductos, producto);
                }
                return false;
            }

            try
            {
                BorrarDependenciasSinMovimiento(IdProductos);
                _context.ChangeTracker.Clear();
                var fresco = _context.Productos.Find(IdProductos);
                if (fresco == null)
                    return true;
                _context.Productos.Remove(fresco);
                _context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();
                var fresco = _context.Productos.Find(IdProductos);
                if (fresco != null && fresco.IsActivo)
                {
                    fresco.IsActivo = false;
                    _context.SaveChanges();
                }
                return false;
            }
        }

        private bool TieneMovimiento(int idProducto)
        {
            const string sql = @"
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM FacturaDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM MovimientosInventarioDetalle WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM MovimientoDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM ConduceDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM OrdenCompraDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM DevolucionesClienteDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM DevolucionesDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM AjusteInventarioDetalles WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM OrdenProduccion WHERE IdProductoTerminado = {0}
    UNION ALL SELECT 1 FROM OrdenProduccionMaterial WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM RecetaProduccion WHERE IdProductoTerminado = {0}
    UNION ALL SELECT 1 FROM RecetaProduccionItem WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM NotasCreditoDetalle WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM Citas WHERE IdProducto = {0}
    UNION ALL SELECT 1 FROM ActivosFijos WHERE IdProducto = {0}
) THEN 1 ELSE 0 END AS [Value]";

            return _context.Database.SqlQueryRaw<int>(sql, idProducto).AsEnumerable().First() == 1;
        }

        private void BorrarDependenciasSinMovimiento(int idProducto)
        {
            _context.Database.ExecuteSqlRaw("DELETE FROM AlmacenExistencias WHERE IdProducto = {0}", idProducto);
            _context.Database.ExecuteSqlRaw("DELETE FROM Variaciones WHERE IdProducto = {0}", idProducto);
            _context.Database.ExecuteSqlRaw("DELETE FROM EmpleadoServicioComisions WHERE IdProducto = {0}", idProducto);
            _context.Database.ExecuteSqlRaw("DELETE FROM DescuentoDetalle WHERE IdProducto = {0}", idProducto);
        }

        public void ActivarProductos(int IdProductos)
        {
            var producto = Repository.GetById(IdProductos)
                ?? throw new InvalidOperationException("Producto no encontrado");
            if (producto.IsActivo)
                return;
            producto.IsActivo = true;
            Repository.Update(IdProductos, producto);
        }

        public async Task<IEnumerable<Productos>> GetAllProductos(int IdEmpresa)
        {
            return await Repository.GetAllByExpresionAsync(c => c.IdEmpresa == IdEmpresa);
        }
        public async Task<IEnumerable<Productos>> GetAllProductosVenta(int IdEmpresa, int? idSucursal = null)
        {
            var productos = (await Repository
                .GetAllByExpresionAsync(
                    c =>
                        c.IdEmpresa == IdEmpresa
                        && c.IsActivo
                        && (
                            c.EsServicio
                            || (
                                c.SeVende
                                && (
                                    c.TipoOperacion == "VENTA"
                                    || c.TipoOperacion == "AMBAS"
                                )
                            )
                        )))
                .ToList();

            await AplicarExistenciaRealAsync(IdEmpresa, productos, idSucursal);
            return productos;
        }

        public async Task<Productos> GetAllProductosById(int IdProductos)
        {
            return await Repository.GetByIdAsync(IdProductos);
        }

        public async Task<IEnumerable<Productos>> GetAllProductosByIdCategoria(int IdCategoria, int IdEmpresa)
        {
            return await Repository.GetAllByExpresionAsync(c => c.IdCategoria == IdCategoria && c.IdEmpresa == IdEmpresa);
        }

        public async Task<Productos> GetProductByBarcCode(string BarCode, int IdEmpresa, int? idSucursal = null)
        {
            var producto = await Repository.GetByExpresionAsync(c =>
                c.CodigoBarra == BarCode
                && c.IdEmpresa == IdEmpresa
                && c.IsActivo
                && c.SeVende);
            if (producto != null)
                await AplicarExistenciaRealAsync(IdEmpresa, new List<Productos> { producto }, idSucursal);
            return producto;
        }

        /// <summary>
        /// Fuente de verdad de stock para POS/venta: AlmacenExistencias (suma).
        /// Si idSucursal tiene valor, solo almacenes de esa sucursal.
        /// Productos.Cantidad en BD es legado y no se reescribe aquí.
        /// Servicios no controlan existencia: Cantidad=0 y ControlarStock=false en la respuesta.
        /// </summary>
        private async Task AplicarExistenciaRealAsync(
            int idEmpresa,
            List<Productos> productos,
            int? idSucursal = null)
        {
            if (productos == null || productos.Count == 0)
                return;

            var idsControlados = productos
                .Where(p => !p.EsServicio && p.ControlarStock)
                .Select(p => p.IdProducto)
                .Distinct()
                .ToList();

            Dictionary<int, decimal> mapa = new();
            if (idsControlados.Count > 0)
            {
                var query = _context.AlmacenExistencia
                    .AsNoTracking()
                    .Where(e =>
                        e.IdEmpresa == idEmpresa
                        && idsControlados.Contains(e.IdProducto));

                if (idSucursal is > 0)
                {
                    var idsAlmacen = await _context.Almacenes
                        .AsNoTracking()
                        .Where(a => a.IdEmpresa == idEmpresa && a.IdSucursal == idSucursal)
                        .Select(a => a.IdAlmacen)
                        .ToListAsync();

                    query = query.Where(e => idsAlmacen.Contains(e.IdAlmacen));
                }

                var filas = await query
                    .GroupBy(e => e.IdProducto)
                    .Select(g => new { IdProducto = g.Key, Total = g.Sum(x => x.Cantidad) })
                    .ToListAsync();

                mapa = filas.ToDictionary(x => x.IdProducto, x => x.Total);
            }

            foreach (var p in productos)
            {
                if (p.EsServicio)
                {
                    // Misconfiguración frecuente: servicio con ControlarStock/Cantidad inventada.
                    p.ControlarStock = false;
                    p.Cantidad = 0;
                    continue;
                }

                if (!p.ControlarStock)
                    continue;

                p.Cantidad = mapa.TryGetValue(p.IdProducto, out var total) ? total : 0m;
            }
        }

        public async Task InsertProductos(Productos Productos)
        {
            await Repository.Save(Productos);
        }

        public void UpdateProductos(int Id, Productos Productos)
        {
            Repository.Update(Id, Productos);
        }

        // 🚀 SERVICIO GENÉRICO BIZCOCHO ENCARGO
        public async Task<Productos?> GetServicioBizcochoEncargo(int idEmpresa)
        {
            return await Repository.GetByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa &&
                x.EsServicio == true &&
                x.Descripcion == "Bizcocho Encargo"
            );
        }
        // 🚀 Nuevo método: traer solo servicios por área

        public async Task<ProductoBusquedaCompraResultDto> BuscarProductosCompra(
            int idEmpresa,
            string? q,
            int page,
            int pageSize,
            int? idAlmacen = null)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 25 : Math.Min(pageSize, 100);

            var query = _context.Productos
                .AsNoTracking()
                .Where(p =>
                    p.IdEmpresa == idEmpresa
                    && p.IsActivo);

            var term = (q ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(term))
            {
                var lower = term.ToLower();
                var esNumerico = int.TryParse(term, out var idProducto);

                query = query.Where(p =>
                    (p.Nombre != null && p.Nombre.ToLower().Contains(lower))
                    || (p.Descripcion != null && p.Descripcion.ToLower().Contains(lower))
                    || (p.CodigoBarra != null && p.CodigoBarra.ToLower().Contains(lower))
                    || (esNumerico && p.IdProducto == idProducto));
            }

            var total = await query.CountAsync();

            var productos = await query
                .OrderBy(p => p.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductoBusquedaCompraDto
                {
                    IdProducto = p.IdProducto,
                    CodigoBarra = p.CodigoBarra,
                    Nombre = p.Nombre,
                    Cantidad = p.Cantidad,
                    PrecioCompra = p.PrecioCompra,
                    ControlarStock = p.ControlarStock,
                    EsServicio = p.EsServicio,
                    TipoComportamiento = p.TipoComportamiento,
                    Itbis = p.Itbis,
                    ExistenciaAlmacen = 0
                })
                .ToListAsync();

            if (idAlmacen.HasValue && idAlmacen.Value > 0 && productos.Count > 0)
            {
                var ids = productos.Select(p => p.IdProducto).ToList();
                var existencias = await _context.AlmacenExistencia
                    .AsNoTracking()
                    .Where(e =>
                        e.IdEmpresa == idEmpresa
                        && e.IdAlmacen == idAlmacen.Value
                        && ids.Contains(e.IdProducto))
                    .ToListAsync();

                var mapa = existencias.ToDictionary(e => e.IdProducto, e => e.Cantidad);
                foreach (var item in productos)
                {
                    if (mapa.TryGetValue(item.IdProducto, out var cant))
                        item.ExistenciaAlmacen = cant;
                }
            }

            return new ProductoBusquedaCompraResultDto
            {
                Items = productos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                HasMore = page * pageSize < total
            };
        }

    }
}
