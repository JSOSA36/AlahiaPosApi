using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpleadoAreaComisionService
    {
        Task<IEnumerable<EmpleadoAreaComision>> GetAllComisionesByEmpleado(int idEmpleado);
        Task<EmpleadoAreaComision> GetComisionById(int id);
        Task InsertComision(EmpleadoAreaComision comision);
        void UpdateComision(int id, EmpleadoAreaComision comision);
        void DeleteComision(int id);
        Task DeleteByEmpleado(int idEmpleado);
        Task InsertRange(IEnumerable<EmpleadoAreaComision> comisiones);
    }
}
