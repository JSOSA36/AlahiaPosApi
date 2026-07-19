using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITesoreriaConfiguracionService
    {
        Task<TesoreriaConfiguracion?> GetByEmpresaAsync(int idEmpresa);
        Task<TesoreriaConfiguracion> UpsertAsync(TesoreriaConfiguracion configuracion);
        Task EnsureConfiguracionAsync(int idEmpresa);
    }
}
