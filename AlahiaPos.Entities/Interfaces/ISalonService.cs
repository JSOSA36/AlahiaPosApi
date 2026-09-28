using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ISalonService
    {
        Task<SalonSnapshotDto> ObtenerSnapshotAsync(int idEmpresa, int? idZona = null);
        Task<IReadOnlyList<SalonOrdenDto>> ObtenerOrdenesMesaAsync(int idEmpresa, int idMesa);
        Task<SalonZonaDto> CrearZonaAsync(int idEmpresa, string nombre);
        Task<SalonZonaDto> RenombrarZonaAsync(int idEmpresa, int zonaId, string nombre);
        Task<SalonMesaDto> CrearMesaAsync(int idEmpresa, int zonaId, string numero, int capacidad);
    }
}
