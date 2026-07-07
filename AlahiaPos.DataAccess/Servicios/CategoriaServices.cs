using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class CategoriaServices : ICategorias
    {

        IRepository<Categorias> _repository;

        public CategoriaServices(IRepository<Categorias> repository)
        {
            _repository = repository;
        }

        public void DeleteCategorias(int IdCategorias)
        {
            _repository.Delete(IdCategorias);
        }
        public async Task<IEnumerable<Categorias>> GetAllCategoriasVentas(int idEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c =>
                c.IdEmpresa == idEmpresa &&
                (c.TipoOperacion == "VENTA" || c.TipoOperacion == "AMBAS") &&
                c.IsActiva
            );
        }
        public async Task<IEnumerable<Categorias>> GetAllCategorias(int IdEmpresa)
        {
            return await _repository.GetAllByExpresionAsync(c => c.Tipo != "Categoria de Compra" 
            && c.IdEmpresa == IdEmpresa && c.IsActiva==true);
        }

        public async Task<Categorias> GetAllCategoriasById(int IdCategorias)
        {
            return await _repository.GetByIdAsync(IdCategorias);
        }

        public async Task InsertCategorias(Categorias Categorias)
        {
            await _repository.Save(Categorias);
        }

        public void UpdateCategorias(int Id, Categorias Categorias)
        {
            _repository.Update(Id, Categorias); 
        }
    }
}
