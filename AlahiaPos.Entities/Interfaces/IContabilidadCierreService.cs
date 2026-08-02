using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadCierreService
    {
        Task<PeriodoContableDto?> GetPeriodoAsync(int idEmpresa, int anio, int mes);
        Task<IEnumerable<PeriodoContableDto>> GetPeriodosAsync(int idEmpresa, int anio);
        Task<PeriodoContableDto> CerrarPeriodoAsync(CerrarPeriodoRequest request);
        Task<bool> EstaPeriodoBloqueadoAsync(int idEmpresa, DateTime fecha);
        Task<DateTime?> ResolverFechaContableAbiertaAsync(int idEmpresa, DateTime preferida);
    }
}
