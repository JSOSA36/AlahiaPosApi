using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporteIr3Service
    {
        /// <summary>
        /// Liquidación IR-3 (retenciones ISR de asalariados) en vivo desde nómina pagada/cerrada.
        /// <paramref name="periodo"/> formato AAAAMM.
        /// </summary>
        Task<ReporteIr3Dto> ObtenerAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? periodo = null);
    }
}
