using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IProductosServices : IProductos
    {
        IRepository<Productos> Repository;

        public IProductosServices(IRepository<Productos> repository)
        {
            Repository = repository;
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
            return await Repository.GetByExpresionAsync(c => c.CodigoBarra == BarCode && c.IdEmpresa == IdEmpresa);
        }

        public async Task InsertProductos(Productos Productos)
        {
            await Repository.Save(Productos);
        }

        public void UpdateProductos(int Id, Productos Productos)
        {
            Repository.Update(Id, Productos);
        }

        // 🚀 Nuevo método: traer solo servicios por área
       
    }
}
