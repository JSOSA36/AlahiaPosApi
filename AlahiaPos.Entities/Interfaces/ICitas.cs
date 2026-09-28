
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICitas
    {
        Task<IEnumerable<CitaDto>> GetAllCitas(int IdEmpresa);

        Task<Cita> GetCitaById(int id);
        Task<Cita> GetCitaByIdFactHeader(int IdFacturaHeader);
        Task EnviarRecordatorioPorFecha(DateTime fecha, int idEmpresa);
        Task<int> EnviarRecordatoriosDelDiaAsync(DateTime fecha, int? idEmpresa = null);
        Task InsertCita(Cita cita);
        void UpdateCita(Cita cita);
        void DeleteCita(int id);
        Task<IEnumerable<Cita>> GetCitasByEmpleado(int idEmpleado, int idEmpresa);
        Task<IEnumerable<Cita>> GetCitasByCliente(int idEmpresa);
        Task<int> TotalCitasDelMes(int idEmpresa);
        /// <summary>
        /// Obtiene todas las citas de la empresa incluyendo el nombre del estilista asociado.
        /// Ideal para vistas o dashboards.
        /// </summary>
        /// <param name="idEmpresa">ID de la empresa</param>
        /// <returns>Listado de citas con nombre del estilista</returns>
        Task<IEnumerable<CitaDto>> GetCitasConEmpleado(int idEmpresa);
        Task CambiarEstadoCita(int idCita, EstadoCita estado, int idUsuario = 0);

    }
}
