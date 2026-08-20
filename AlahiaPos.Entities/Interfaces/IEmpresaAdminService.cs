using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpresaAdminService
    {
        Task<List<EmpresaAdminListItemDto>> ListarAsync();
        Task<EmpresaAdminDetalleDto?> ObtenerAsync(int idEmpresa);
        Task<List<ModuloCatalogoItemDto>> CatalogoModulosAsync(int? idEmpresaSeleccion = null);
        Task<List<EmpresaAdminVerticalPresetDto>> ListarVerticalesAsync();
        Task<EmpresaAdminAltaResultDto> AltaAsync(EmpresaAdminAltaRequest req);
        Task ActualizarDemoAsync(int idEmpresa, EmpresaAdminDemoRequest req);
        Task ActualizarNivelSoporteAsync(int idEmpresa, EmpresaAdminNivelSoporteRequest req);
        Task SincronizarModulosAsync(int idEmpresa, EmpresaAdminModulosRequest req);
    }
}
