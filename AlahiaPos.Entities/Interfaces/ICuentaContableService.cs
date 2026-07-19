using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICuentaContableService
    {
        Task<IEnumerable<CuentaContable>> GetByEmpresaAsync(int idEmpresa);
        Task<IEnumerable<CuentaContable>> GetArbolByEmpresaAsync(int idEmpresa);
        Task<CuentaContable?> GetByIdAsync(int id, int idEmpresa);
        Task<int> CreateAsync(CuentaContable entity);
        Task UpdateAsync(CuentaContable entity);
        Task DeleteAsync(int id, int idEmpresa);
    }
}
