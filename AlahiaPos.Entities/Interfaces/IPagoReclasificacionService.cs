using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPagoReclasificacionService
    {
        Task<ReclasificarPagoResultadoDto> ReclasificarDesdeConciliacionAsync(ReclasificarPagoConciliacionDto dto);
        Task<ReclasificarPagoResultadoDto> ReversarAsync(ReversarReclasificacionPagoDto dto);
        Task<Dictionary<int, string>> ObtenerMetodosEfectivosAntesCierreAsync(int idEmpresa, IEnumerable<int> idIngresos);
        Task<Dictionary<int, string>> ObtenerMetodosEfectivosPorFacturaAntesCierreAsync(int idEmpresa, IEnumerable<int> idFacturas);
        Task<IReadOnlyList<PagoReclasificacion>> ListarActivasPorEmpresaAsync(int idEmpresa, DateTime? desde = null, DateTime? hasta = null);
    }
}
