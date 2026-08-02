using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IComprasService
    {
        Task<FacturaCompraDto> GuardarBorradorAsync(GuardarFacturaCompraRequest request);
        Task<FacturaCompraDto> ConfirmarAsync(int idOrdenCompraHeader, ConfirmarFacturaCompraRequest request);
        Task<FacturaCompraDto?> ObtenerPorIdAsync(int idOrdenCompraHeader, int idEmpresa);
        Task<IEnumerable<FacturaCompraDto>> ListarAsync(
            int idEmpresa,
            string? estado = null,
            DateTime? desde = null,
            DateTime? hasta = null);
        Task<IEnumerable<FacturaCompraDto>> ListarPendientesAsync(
            int idEmpresa,
            int? idProveedor = null,
            DateTime? desde = null,
            DateTime? hasta = null);
        Task<FacturaCompraDto> RegistrarPagoAsync(int idOrdenCompraHeader, RegistrarPagoProveedorRequest request);
        Task<IEnumerable<PagosProveedor>> ObtenerPagosAsync(int idOrdenCompraHeader, int idEmpresa);
        Task AnularAsync(int idOrdenCompraHeader, int idEmpresa);

        Task<IEnumerable<FacturaCompraDto>> BuscarPendientesRecepcionAsync(
            int idEmpresa,
            string? texto = null,
            int? idProveedor = null);

        Task<FacturaCompraDto?> ObtenerParaRecepcionAsync(int idOrdenCompraHeader, int idEmpresa);

        Task<FacturaCompraDto> ConfirmarRecepcionAsync(
            int idOrdenCompraHeader,
            ConfirmarRecepcionCompraRequest request);

        // --- Orden de Compra (tipo 5) ---
        Task<FacturaCompraDto> GuardarBorradorOrdenAsync(GuardarFacturaCompraRequest request);
        Task<FacturaCompraDto> EmitirOrdenAsync(int idOrdenCompraHeader, EmitirOrdenCompraRequest request);
        Task<IEnumerable<FacturaCompraDto>> ListarOrdenesAsync(
            int idEmpresa,
            string? estado = null,
            DateTime? desde = null,
            DateTime? hasta = null);
        Task<FacturaCompraDto> GenerarFacturaDesdeOrdenAsync(int idOrdenCompraHeader, int idEmpresa, int idUsuario);
        Task<FacturaCompraDto> MarcarOrdenEnviadaAsync(int idOrdenCompraHeader, EnviarOrdenCompraRequest request);

        Task<EstadoCuentaProveedorDto> ObtenerEstadoCuentaProveedorAsync(
            int idEmpresa,
            int idProveedor,
            DateTime desde,
            DateTime hasta);

        Task<AnalisisProductoProveedorDto> ObtenerAnalisisProductoProveedorAsync(
            AnalisisCompraFiltroRequest filtro);

        Task<Reporte606Dto> ObtenerReporte606Async(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            string? periodo = null);
    }
}
