using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDashboardGerencialService
    {
        Task<DashboardGerencialDto> ObtenerMesActualAsync(int idEmpresa);
    }
}
