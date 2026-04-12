using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IZonas
    {
        Task<IEnumerable<Zonas>> GetAllZonas();
        Task<Zonas> GetAllZonasById(int IdZonas);
        void UpdateZonas(int Id, Zonas Zonas);
        Task InsertZonas(Zonas Zonas);
        void DeleteZonas(int IdMesa);
    }
}
