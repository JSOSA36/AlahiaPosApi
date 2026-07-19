using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ModulosServices : IModulo
    {
        private readonly IRepository<Modulo> _Repository;

        public ModulosServices(IRepository<Modulo> repository)
        {
            _Repository = repository;
        }

        public async Task<IEnumerable<Modulo>> GetAllModulos()
        {
            return await _Repository.GetAllAsync();
        }

        public async Task<Modulo> GetModuloById(int IdModulo)
        {
            return await _Repository.GetByIdAsync(IdModulo);
        }

        public async Task<Modulo?> GetModuloByCodigo(string Codigo)
        {
            return await _Repository.GetByExpresionAsync(
                c => c.Codigo == Codigo && c.Activo == true
            );
        }

        public async Task InsertModulo(Modulo modulo)
        {
            await _Repository.Save(modulo);
        }

        public void UpdateModulo(int IdModulo, Modulo modulo)
        {
            _Repository.Update(IdModulo, modulo);
        }

        public void DeleteModulo(int IdModulo)
        {
            _Repository.Delete(IdModulo);
        }
    }
}
