using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IImpresorasZonas
    {
        Task<IEnumerable<ImpresorasZonas>> GetImpresorasZonas();
        Task<ImpresorasZonas> GetAllImpresorasZonasById(int IdImpresorasZonas);
        void UpdateImpresorasZonas(int Id, ImpresorasZonas ImpresorasZonas);
        Task InsertImpresorasZonas(ImpresorasZonas ImpresorasZonas);
        void DeleteImpresorasZonas(int IdImpresorasZonas);
    }
}
