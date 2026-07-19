using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadLibrosService
    {
        Task<IEnumerable<LibroDiarioLineaDto>> GetLibroDiarioAsync(int idEmpresa, DateTime desde, DateTime hasta);
        Task<MayorGeneralResumenDto?> GetMayorGeneralAsync(int idEmpresa, int idCuentaContable, DateTime desde, DateTime hasta);
        Task<IEnumerable<MayorGeneralResumenDto>> GetMayorGeneralResumenAsync(int idEmpresa, DateTime desde, DateTime hasta);
    }
}
