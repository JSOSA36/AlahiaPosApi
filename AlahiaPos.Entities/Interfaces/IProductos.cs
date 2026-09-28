using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IProductos
    {
        Task<IEnumerable<Productos>> GetAllProductos(int IdEmpresa);
        Task<IEnumerable<Productos>> GetAllProductosVenta(int IdEmpresa, int? idSucursal = null);
        Task<Productos?> GetServicioBizcochoEncargo(int idEmpresa);
        Task<IEnumerable<Productos>> GetAllProductosByIdCategoria(int IdCategoria, int IdEmpresa);
        Task<Productos> GetProductByBarcCode(string BarCode, int IdEmpresa, int? idSucursal = null);
        Task<IEnumerable<Productos>> GetServiciosByArea(int idArea, int idEmpresa);
        Task<Productos> GetAllProductosById(int IdProductos);
        Productos GetProductoById(int Id);
        void UpdateProductos(int Id, Productos CateProductosgorias);
        Task InsertProductos(Productos Productos);
        /// <summary>
        /// Borra el producto si no tiene movimiento.
        /// Si ya se usó en un documento, lo inactiva.
        /// </summary>
        /// <returns>true si se eliminó la fila; false si quedó inactivo.</returns>
        bool DeleteProductos(int IdProductos);

        /// <summary>Reactiva un producto desactivado.</summary>
        void ActivarProductos(int IdProductos);
        Task<IEnumerable<ProductoLiteDto>> GetLite(int IdEmpresa);
        Task<ProductoBusquedaCompraResultDto> BuscarProductosCompra(
            int idEmpresa,
            string? q,
            int page,
            int pageSize,
            int? idAlmacen = null);


    }
}
