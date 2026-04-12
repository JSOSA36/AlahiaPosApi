using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICategorias
    {
        Task<IEnumerable<Categorias>> GetAllCategorias(int IdEmpresa);
        
        Task<Categorias> GetAllCategoriasById(int IdCategorias);
        void UpdateCategorias(int Id, Categorias Categorias);
        Task InsertCategorias(Categorias Categorias);
        void DeleteCategorias(int IdCategorias);
    }
}
