using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICitasPublicasService
    {
        Task<CitaPublicaSalonDto> ObtenerSalonAsync(string guid);
        Task<CitaPublicaDisponibilidadDto> ObtenerDisponibilidadAsync(string guid, int idEmpleado, string fecha);
        Task<CitaPublicaConfirmacionDto> CrearCitaAsync(string guid, CitaPublicaCrearRequest request);
        Task<List<CitaPublicaItemDto>> ListarCitasClienteAsync(string guid, string telefono);
    }
}
