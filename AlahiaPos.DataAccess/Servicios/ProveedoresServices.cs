using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ProveedoresServices : IProveedores
    {

        IRepository<Proveedores> _repository;
        public ProveedoresServices(IRepository<Proveedores> repository)
        {
            _repository = repository;
        }

        public void DeleteProveedores(int IdProveedores)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Proveedores>> GetAllProveedores()
        {
            return await _repository.GetAllAsync();
        }

        public Task<Proveedores> GetAllProveedoresById(int IdCategorias)
        {
            throw new NotImplementedException();
        }

        public Task InsertProveedores(Proveedores Proveedores)
        {
            throw new NotImplementedException();
        }

        public void UpdateProveedores(int Id, Proveedores Proveedores)
        {
            throw new NotImplementedException();
        }
    }
}
