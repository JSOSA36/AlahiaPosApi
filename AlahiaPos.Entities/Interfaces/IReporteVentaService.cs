using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporteVentaService
    {
        Task<ReporteVentaFacturasDto> ObtenerFacturasAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            SucursalConsultaScope? consulta = null);
        Task<ReporteVentaProductosDto> ObtenerProductosAsync(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            SucursalConsultaScope? consulta = null);
    }
}
