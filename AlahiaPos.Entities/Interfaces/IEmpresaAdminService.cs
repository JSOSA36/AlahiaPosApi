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
        Task ActualizarDatosAsync(int idEmpresa, EmpresaAdminDatosRequest req);
        Task ActualizarNivelSoporteAsync(int idEmpresa, EmpresaAdminNivelSoporteRequest req);
        Task ActualizarTrabajaDomingoAsync(int idEmpresa, EmpresaAdminTrabajaDomingoRequest req);
        Task SincronizarModulosAsync(int idEmpresa, EmpresaAdminModulosRequest req);
        Task<List<EmpresaAdminPerfilDto>> ListarPerfilesAsync(int idEmpresa);
        Task<EmpresaAdminPerfilDto> CrearPerfilAsync(int idEmpresa, EmpresaAdminPerfilRequest req);
        Task<EmpresaAdminPerfilDto> ActualizarPerfilAsync(int idEmpresa, int idPerfil, EmpresaAdminPerfilRequest req);
        Task EliminarPerfilAsync(int idEmpresa, int idPerfil);
    }
}
