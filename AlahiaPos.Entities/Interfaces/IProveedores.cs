using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IProveedores
    {
        public Task<IEnumerable<Proveedores>> GetAllProveedores();

        public Task<Proveedores> GetAllProveedoresById(int IdCategorias);
        public void UpdateProveedores(int Id, Proveedores Proveedores);
        public Task InsertProveedores(Proveedores Proveedores);
        public void DeleteProveedores(int IdProveedores);
    }
}
