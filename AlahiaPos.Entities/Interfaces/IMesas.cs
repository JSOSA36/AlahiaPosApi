using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IMesas
    {
        Task<IEnumerable<Mesas>> GetAllMesasByZona(int IdZona);
        Task<Mesas> GetAllMesasById(int IdMesa);
        void UpdateMesas(int Id, Mesas mesas);
    }
}
