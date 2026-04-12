using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class IMesasServices : IMesas
    {
        IRepository<Mesas> _services;
        public IMesasServices(IRepository<Mesas> services)
        {
            _services = services;
        }

        public void DeleteMesa(int IdMesa)
        {
          
             _services.Delete(IdMesa);
        }

        public async Task<IEnumerable<Mesas>> GetAllMesas()
        {
            return await _services.GetAllAsync();
        }

        public async Task<Mesas> GetAllMesasById(int IdMesa)
        {
            return await _services.GetByIdAsync(IdMesa);
        }

        public async Task<IEnumerable<Mesas>> GetAllMesasByZona(int IdZona)
        {
            return await _services.GetAllByExpresionAsync(c => c.ZonaId==IdZona);
        }

        public async Task InsertMesa(Mesas mesas)
        {
             await _services.Save(mesas);
        }

        public void UpdateMesas(int Id, Mesas mesas)
        {
            _services.Update(Id, mesas);
        }
    }
}
