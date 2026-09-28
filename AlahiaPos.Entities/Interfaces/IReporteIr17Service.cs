using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporteIr17Service
    {
        /// <summary>
        /// Genera liquidación IR-17 2026 (Julio 2026 en adelante) en vivo para el periodo.
        /// <paramref name="periodo"/> formato AAAAMM.
        /// </summary>
        Task<ReporteIr17Dto> ObtenerAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? periodo = null);
    }
}
