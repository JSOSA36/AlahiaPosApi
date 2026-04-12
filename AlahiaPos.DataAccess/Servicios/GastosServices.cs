using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class GastosServices : IGastos
    {
        IRepository<Gastos> _services;

        public GastosServices(IRepository<Gastos> services)
        {
            _services = services;
        }

        public void DeleteGastos(int id)
        {
            _services.Delete(id);
        }

        public Task<IEnumerable<Gastos>> GetAllGastos(int IdEmpresa)
        {
            return _services.GetAllByExpresionAsync(c=>c.IdEmpresa==IdEmpresa);
        }

        public Task<Gastos> GetGastosById(int id)
        {
           return _services.GetByIdAsync(id);
        }

        public Task InsertGastos(Gastos Gastos)
        {
            return _services.Save(Gastos);
        }

        public async Task<decimal> TotalGastosDelMes(int IdEmpresa)
        {
            var result = await  _services.GetAllByExpresionAsync(c =>
             c.FechaInseccion.Year == DateTime.Now.Year
              && c.FechaInseccion.Month == DateTime.Now.Month &&
              c.IdEmpresa == IdEmpresa);
            if (result != null)
            {
                return result.Sum(c => c.Monto);
            }
            else
            {
                return 0;
            }
        }

        public void UpdateGastos(Gastos Gastos)
        {
            throw new NotImplementedException();
        }
    }
}
