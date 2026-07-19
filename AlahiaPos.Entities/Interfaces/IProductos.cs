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
        Task<IEnumerable<Productos>> GetAllProductosVenta(int IdEmpresa);
        Task<Productos?> GetServicioBizcochoEncargo(int idEmpresa);
        Task<IEnumerable<Productos>> GetAllProductosByIdCategoria(int IdCategoria, int IdEmpresa);
        Task<Productos> GetProductByBarcCode(string BarCode, int IdEmpresa);
        Task<IEnumerable<Productos>> GetServiciosByArea(int idArea, int idEmpresa);
        Task<Productos> GetAllProductosById(int IdProductos);
        Productos GetProductoById(int Id);
        void UpdateProductos(int Id, Productos CateProductosgorias);
        Task InsertProductos(Productos Productos);
        void DeleteProductos(int IdProductos);
        Task<IEnumerable<ProductoLiteDto>> GetLite(int IdEmpresa);
        Task<ProductoBusquedaCompraResultDto> BuscarProductosCompra(
            int idEmpresa,
            string? q,
            int page,
            int pageSize,
            int? idAlmacen = null);


    }
}
