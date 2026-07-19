using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IAsientoContableService
    {
        Task<IEnumerable<AsientoContable>> GetByEmpresaAsync(int idEmpresa, DateTime? desde = null, DateTime? hasta = null);
        Task<IEnumerable<AsientoContable>> ConsultarAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            int? idCuentaContable = null,
            string? numero = null,
            string? concepto = null);
        Task<AsientoContable?> GetByIdAsync(int id, int idEmpresa);
        Task<int> CreateAsync(AsientoContable asiento);
        Task<int> CreateAutomaticoAsync(AsientoContable asiento);
        Task UpdateAsync(AsientoContable asiento);
        Task AnularAsync(int id, int idEmpresa);
    }
}
