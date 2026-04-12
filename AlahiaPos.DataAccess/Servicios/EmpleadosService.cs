using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpleadosService : IEmpleados
    {
        private readonly IRepository<Empleados> _empleadosRepository;

        public EmpleadosService(IRepository<Empleados> empleadosRepository)
        {
            _empleadosRepository = empleadosRepository;
        }

        // =====================================================
        // 👤 LISTAR EMPLEADOS POR EMPRESA
        // =====================================================
        public async Task<IEnumerable<Empleados>> GetAllEmpleados(int empresaId)
        {
            return await _empleadosRepository.GetAllByExpresionAsync(
                e => e.IdEmpresa == empresaId && e.Estado
            );
        }

        // =====================================================
        // 👤 OBTENER EMPLEADO POR ID
        // =====================================================
        public async Task<Empleados?> GetEmpleadoById(int idEmpleado)
        {
            return await _empleadosRepository.GetByIdAsync(idEmpleado);
        }
        

        // =====================================================
        // 📧 OBTENER EMPLEADO POR CORREO (SOLO REFERENCIAL)
        // =====================================================


        // =====================================================
        // ➕ INSERTAR EMPLEADO
        // =====================================================
        public async Task InsertEmpleados(Empleados empleado)
        {
            await _empleadosRepository.Save(empleado);
        }

        // =====================================================
        // ✏️ ACTUALIZAR EMPLEADO
        // =====================================================
        public void UpdateEmpleados(int idEmpleado, Empleados empleado)
        {
            _empleadosRepository.Update(idEmpleado, empleado);
        }

        // =====================================================
        // ❌ ELIMINAR EMPLEADO (LÓGICO)
        // =====================================================
        public void DeleteEmpleados(int idEmpleado)
        {
            var empleado = _empleadosRepository.GetById(idEmpleado);

            if (empleado != null)
            {
                empleado.Estado = false;
                _empleadosRepository.Update(idEmpleado, empleado);
            }
        }
    }
}
