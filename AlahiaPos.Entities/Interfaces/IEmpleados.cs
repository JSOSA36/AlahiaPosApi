using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpleados
    {
        // =====================================================
        // 👤 CRUD EMPLEADOS
        // =====================================================

        Task<IEnumerable<Empleados>> GetAllEmpleados(int empresaId);
        Task<Empleados?> GetEmpleadoById(int idEmpleado);
        
        Task InsertEmpleados(Empleados empleado);
        void UpdateEmpleados(int idEmpleado, Empleados empleado);
        void DeleteEmpleados(int idEmpleado);

       
        // =====================================================
        // 🔐 PASSWORD / RECUPERACIÓN
        // =====================================================

       
    }
}
