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
                c.EsServicio == true // 👈 criterio: que sean servicios
            );
        }

        public Productos GetProductoById(int Id)
        {
            return Repository.GetById(Id);
        }

        public void DeleteProductos(int IdProductos)
        {
            Repository.Delete(IdProductos);
        }

        public async Task<IEnumerable<Productos>> GetAllProductos(int IdEmpresa)
        {
            return await Repository.GetAllByExpresionAsync(c => c.IdEmpresa == IdEmpresa);
        }
        public async Task<IEnumerable<Productos>> GetAllProductosVenta(int IdEmpresa)
        {
            var productos = (await Repository
                .GetAllByExpresionAsync(
                    c =>
                        c.IdEmpresa == IdEmpresa
                        && (
                            c.EsServicio
                            || c.TipoOperacion == "VENTA"
                            || c.TipoOperacion == "AMBAS"
                        )))
                .ToList();

            await AplicarExistenciaRealAsync(IdEmpresa, productos);
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

        public async Task<Productos> GetProductByBarcCode(string BarCode, int IdEmpresa)
        {
            var producto = await Repository.GetByExpresionAsync(c => c.CodigoBarra == BarCode && c.IdEmpresa == IdEmpresa);
            if (producto != null)
                await AplicarExistenciaRealAsync(IdEmpresa, new List<Productos> { producto });
            return producto;
        }

        /// <summary>
        /// Fuente de verdad de stock para POS/venta: AlmacenExistencias (suma).
        /// Productos.Cantidad es legado y no debe mostrarse como disponible.
        /// Servicios no controlan existencia: Cantidad=0 y ControlarStock=false en la respuesta.
        /// </summary>
        private async Task AplicarExistenciaRealAsync(int idEmpresa, List<Productos> productos)
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
                var filas = await _context.AlmacenExistencia
                    .AsNoTracking()
                    .Where(e =>
                        e.IdEmpresa == idEmpresa
                        && idsControlados.Contains(e.IdProducto))
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
