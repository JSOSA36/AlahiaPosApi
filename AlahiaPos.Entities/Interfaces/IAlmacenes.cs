using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IAlmacenes
    {
        Task<IEnumerable<Almacen>> GetAllAlmacenes(int idEmpresa);
        Task<Almacen?> GetAlmacenById(int idAlmacen);
        Task<Almacen?> GetAlmacenPrincipal(int idEmpresa);
        Task InsertAlmacen(Almacen almacen);
        Task UpdateAlmacen(int id, Almacen almacen);
        Task DeleteAlmacen(int idAlmacen);
    }
}
