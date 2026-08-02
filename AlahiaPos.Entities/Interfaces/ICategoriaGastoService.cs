using AlahiaPos.Entities.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ICategoriaGastoService
    {
        Task<IEnumerable<CategoriaGasto>> GetByEmpresaAsync(int idEmpresa, bool soloActivos = false);
        Task EnsureDefaultsAsync(int idEmpresa);
        Task<CategoriaGasto?> GetByIdAsync(int id);
        Task<CategoriaGasto> CreateAsync(CategoriaGasto entity);
        Task UpdateAsync(CategoriaGasto entity);
        Task SoftDeleteAsync(int id);
    }
}
