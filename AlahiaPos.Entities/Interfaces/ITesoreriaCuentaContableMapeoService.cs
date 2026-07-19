using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITesoreriaCuentaContableMapeoService
    {
        Task<IEnumerable<TesoreriaCuentaContableMapeo>> GetByEmpresaAsync(int idEmpresa);
        Task<TesoreriaCuentaContableMapeo?> GetByCuentaFinancieraAsync(int idEmpresa, int idCuentaFinanciera);
        Task<int> UpsertAsync(TesoreriaCuentaContableMapeo mapeo);
        Task DeleteAsync(int id);
    }
}
