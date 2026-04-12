using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class EmpleadoAreaComisionService : IEmpleadoAreaComisionService
    {
        private readonly IRepository<EmpleadoAreaComision> _repository;

        public EmpleadoAreaComisionService(IRepository<EmpleadoAreaComision> repository)
        {
            _repository = repository;
        }

        // 🔹 Obtener todas las comisiones por empleado
        public async Task<IEnumerable<EmpleadoAreaComision>> GetAllComisionesByEmpleado(int idEmpleado)
        {
            return await _repository.GetAllByExpresionAsync(c => c.IdEmpleado == idEmpleado);
        }

        // 🔹 Obtener una comisión específica
        public async Task<EmpleadoAreaComision> GetComisionById(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        // 🔹 Insertar una sola comisión
        public async Task InsertComision(EmpleadoAreaComision comision)
        {
            await _repository.Save(comision);
        }

        // 🔹 Insertar o reemplazar todas las comisiones del empleado
        public async Task InsertRange(IEnumerable<EmpleadoAreaComision> comisiones)
        {
            if (comisiones == null || !comisiones.Any())
                return;

            int idEmpleado = (int)comisiones.First().IdEmpleado;

            // 🔸 Eliminar comisiones anteriores del empleado antes de guardar nuevas
            var existentes = await _repository.GetAllByExpresionAsync(c => c.IdEmpleado == idEmpleado);
            foreach (var item in existentes)
                _repository.Delete(item.IdEmpleadoAreaComision);

            // 🔸 Insertar las nuevas comisiones
            await _repository.Save(comisiones);
        }

        // 🔹 Actualizar una comisión específica
        public void UpdateComision(int id, EmpleadoAreaComision comision)
        {
            _repository.Update(id, comision);
        }

        // 🔹 Eliminar una comisión específica
        public void DeleteComision(int id)
        {
            _repository.Delete(id);
        }

        // 🔹 Eliminar todas las comisiones de un empleado
        public async Task DeleteByEmpleado(int idEmpleado)
        {
            var comisiones = await _repository.GetAllByExpresionAsync(c => c.IdEmpleado == idEmpleado);
            foreach (var item in comisiones)
                _repository.Delete(item.IdEmpleadoAreaComision);
        }
    }
}
