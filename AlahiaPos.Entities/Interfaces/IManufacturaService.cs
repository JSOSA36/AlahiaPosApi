using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IManufacturaService
    {
        Task<List<RecetaDto>> ListarRecetasAsync(int idEmpresa, int? idProducto = null, bool soloActivas = false);
        Task<RecetaDto?> ObtenerRecetaAsync(int idEmpresa, int idReceta);
        Task<RecetaDto> GuardarRecetaAsync(GuardarRecetaRequest request);
        Task<ExplosionDto> ExplotarAsync(int idEmpresa, int idReceta, decimal cantidad, int? idAlmacenOrigen);

        Task<List<OrdenProduccionDto>> ListarOrdenesAsync(int idEmpresa, string? estado = null);
        Task<OrdenProduccionDto?> ObtenerOrdenAsync(int idEmpresa, int idOrden);
        Task<OrdenProduccionDto> GuardarOrdenAsync(GuardarOrdenProduccionRequest request);
        Task<OrdenProduccionDto> PlanificarAsync(int idEmpresa, int idOrden, int idUsuario);
        Task<OrdenProduccionDto> IniciarAsync(int idEmpresa, int idOrden, int idUsuario);
        Task<OrdenProduccionDto> CompletarAsync(int idOrden, CompletarOrdenProduccionRequest request);
        Task<OrdenProduccionDto> CancelarAsync(int idEmpresa, int idOrden, int idUsuario);
        Task<RequerimientoCompraResultadoDto> CrearRequerimientoCompraAsync(int idEmpresa, int idOrden, int idUsuario);
    }
}
