using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IClientes
    {
        public Task<IEnumerable<Clientes>> GetAllClientes(int IdEmpresa);
        Task<IEnumerable<Clientes>> BuscarPorNombre(string nombre, int IdEmpresa);
        public Task<Clientes> GetAllClientesById(int IdClientes);
        public void UpdateClientes(int Id, Clientes Clientes);
        public Task InsertClientes(Clientes Clientes);
        public void DeleteClientes(int IdClientes);
        Task<Clientes?> BuscarPorTelefono(string telefono, int IdEmpresa);


    }
}
