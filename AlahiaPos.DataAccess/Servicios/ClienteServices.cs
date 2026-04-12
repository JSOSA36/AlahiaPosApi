using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class ClienteServices : IClientes
    {

        IRepository<Clientes> _Repository;
     

        public ClienteServices(IRepository<Clientes> repository)
        {
            _Repository = repository;
        }

        public void DeleteClientes(int IdClientes)
        {
            _Repository.Delete(IdClientes);
        }

        public async Task<IEnumerable<Clientes>> GetAllClientes(int IdEmpresa)
        {
            return await _Repository.GetAllByExpresionAsync(c=>c.IdEmpresa==IdEmpresa);
        }

        public async Task<Clientes> GetAllClientesById(int IdClientes)
        {
            return await _Repository.GetByIdAsync(IdClientes);
        }
        public async Task<Clientes?> BuscarPorTelefono(string telefono, int IdEmpresa)
        {
            telefono = telefono.Trim();

            var cliente = await _Repository.GetByExpresionAsync(
                c => c.IdEmpresa == IdEmpresa &&
                     c.Telefono == telefono
            );

            return cliente;
        }

        public async Task<IEnumerable<Clientes>> BuscarPorNombre(string nombre, int IdEmpresa)
        {
            nombre = nombre.ToLower().Trim();

            var lista = await _Repository.GetAllByExpresionAsync(
                c => c.IdEmpresa == IdEmpresa &&
                     c.NombreComercial.ToLower().Contains(nombre)
            );

            return lista;
        }
        public async Task InsertClientes(Clientes Clientes)
        {
             await _Repository.Save(Clientes);
        }

        public void UpdateClientes(int Id, Clientes Clientes)
        {
            _Repository.Update(Id, Clientes);
        }

    }
}
