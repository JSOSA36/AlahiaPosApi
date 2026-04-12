using AlahiaPos.Entities.Domain;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IHorariosEstilista
    {
        // 🔹 Obtener todos los horarios de un estilista
        Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpleado(int idEmpleado );
        Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpleadoByEmpresa(int idEmpleado, int IdEmpresa);

        // 🔹 Obtener todos los horarios de la empresa
        Task<IEnumerable<HorariosEstilista>> GetHorariosByEmpresa(int idEmpresa);

        // 🔹 Insertar nuevo horario
        Task InsertHorario(HorariosEstilista horario);

        // 🔹 Actualizar horario existente
        void UpdateHorario(HorariosEstilista horario);

        // 🔹 Eliminar un horario
        void DeleteHorario(int idHorario);

        // 🔹 Consultar disponibilidad (horas libres de un estilista en una fecha)
        Task<IEnumerable<string>> GetDisponibilidad(int idEmpleado, DateTime fecha);

        // 🔹 Verificar si una hora está disponible (sin conflictos de citas)
        Task<bool> IsHoraDisponible(int idEmpleado, DateTime fecha, TimeSpan hora);
    }
}
