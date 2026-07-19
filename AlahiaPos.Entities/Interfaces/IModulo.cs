using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IModulo
    {
        Task<IEnumerable<Modulo>> GetAllModulos();
        Task<Modulo> GetModuloById(int IdModulo);
        Task<Modulo?> GetModuloByCodigo(string codigo);
        Task InsertModulo(Modulo modulo);
        void UpdateModulo(int IdModulo, Modulo modulo);
        void DeleteModulo(int IdModulo);
    }
}
