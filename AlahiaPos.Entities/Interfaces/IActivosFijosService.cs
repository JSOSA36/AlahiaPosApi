using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IActivosFijosService
    {
        Task<IEnumerable<ActivoFijoDto>> ListarAsync(int idEmpresa, string? estado = null, string? texto = null);
        Task<ActivoFijoDto?> ObtenerPorIdAsync(int idActivoFijo, int idEmpresa);
        Task<ActivoFijoDto> ActualizarAsync(int idActivoFijo, ActualizarActivoFijoRequest request);
        Task<ResumenActivosFijosDto> ObtenerResumenAsync(int idEmpresa);
    }
}
