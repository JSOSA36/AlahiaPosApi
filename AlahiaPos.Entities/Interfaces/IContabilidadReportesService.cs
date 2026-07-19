using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadReportesService
    {
        Task<BalanceComprobacionResumenDto> GetBalanceComprobacionAsync(int idEmpresa, DateTime desde, DateTime hasta);
        Task<EstadoResultadosDto> GetEstadoResultadosAsync(int idEmpresa, DateTime desde, DateTime hasta);
        Task<BalanceGeneralDto> GetBalanceGeneralAsync(int idEmpresa, DateTime fechaCorte);
    }
}
