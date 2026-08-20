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
            telefono = (telefono ?? "").Trim();
            if (string.IsNullOrWhiteSpace(telefono) || IdEmpresa <= 0)
                return null;

            var digitos = new string(telefono.Where(char.IsDigit).ToArray());
            if (digitos.Length < 7)
                return null;

            // Exacto en Telefono o Celular (como se digita o solo dígitos)
            var cliente = await _Repository.GetByExpresionAsync(
                c => c.IdEmpresa == IdEmpresa && (
                    c.Telefono == telefono
                    || c.Celular == telefono
                    || c.Telefono == digitos
                    || c.Celular == digitos
                )
            );
            if (cliente != null)
                return cliente;

            // Fallback: números guardados con guiones/espacios
            var candidatos = await _Repository.GetAllByExpresionAsync(
                c => c.IdEmpresa == IdEmpresa
                     && (
                         (c.Telefono != null && c.Telefono != "")
                         || (c.Celular != null && c.Celular != "")
                     )
            );

            return candidatos.FirstOrDefault(c =>
            {
                var tel = new string((c.Telefono ?? "").Where(char.IsDigit).ToArray());
                var cel = new string((c.Celular ?? "").Where(char.IsDigit).ToArray());
                if (tel == digitos || cel == digitos)
                    return true;
                if (digitos.Length >= 10)
                {
                    var last10 = digitos[^10..];
                    return tel.EndsWith(last10) || cel.EndsWith(last10);
                }
                return tel.EndsWith(digitos) || cel.EndsWith(digitos);
            });
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
