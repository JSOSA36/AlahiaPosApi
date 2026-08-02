using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IReporteIt1Service
    {
        /// <summary>
        /// Genera liquidación IT-1 + Anexo A en vivo para el periodo.
        /// <paramref name="periodo"/> formato AAAAMM; si null se deriva de <paramref name="hasta"/>.
        /// </summary>
        Task<ReporteIt1Dto> ObtenerAsync(
            int idEmpresa,
            DateTime? desde = null,
            DateTime? hasta = null,
            string? periodo = null);
    }
}
