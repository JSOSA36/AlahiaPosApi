using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IAreas
    {
        Task<IEnumerable<Area>> GetAllAreas(int IdEmpresa);
        Task<Area> GetAreaById(int IdArea);
        Task InsertArea(Area area);
        void UpdateArea(int Id, Area area);
        void DeleteArea(int IdArea);
    }
}
