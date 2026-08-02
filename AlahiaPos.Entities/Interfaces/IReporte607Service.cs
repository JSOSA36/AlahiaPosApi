using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporte607Service
    {
        Task<Reporte607Dto> ObtenerReporte607Async(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            string? periodo = null);
    }
}
