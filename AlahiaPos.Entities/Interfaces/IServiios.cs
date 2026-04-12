using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IServicios
    {
        Task<IEnumerable<Productos>> GetAllServicios(int IdEmpresa);
        Task<Productos> GetServicioById(int IdServicio);
        Task InsertServicio(Productos servicio);
        void UpdateServicio(int Id, Productos servicio);
        void DeleteServicio(int IdServicio);
    }
}
