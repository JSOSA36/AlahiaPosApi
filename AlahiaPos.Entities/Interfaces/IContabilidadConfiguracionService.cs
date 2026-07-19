using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IContabilidadConfiguracionService
    {
        Task<ContabilidadConfiguracionDto> GetConfiguracionAsync(int idEmpresa);
        Task<ContabilidadConfiguracionDto> ActualizarAsync(ActualizarContabilidadConfiguracionRequest request);
        Task<ContabilidadConfiguracion> EnsureConfiguracionAsync(int idEmpresa);
        Task InicializarSiEsModuloContabilidadAsync(int idEmpresa, int moduloId, bool activo);
    }
}
