using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Motor de integración contable. Solo debe ser invocado por consumidores de eventos (Contabilidad).
    /// </summary>
    public interface IContabilidadIntegracionService
    {
        Task<int> RegistrarAsientoAutomaticoAsync(ContabilidadIntegracionRequest request);

        /// <summary>
        /// Revierte un asiento automático específico (TipoOperacion original).
        /// El reverso se guarda como REVERSO_{tipoOperacion} para permitir ALTA+COGS.
        /// </summary>
        Task<int> RevertirAsientoAutomaticoAsync(
            int idEmpresa,
            string origenModulo,
            int origenReferenciaId,
            string tipoOperacion,
            int idUsuario,
            string? motivo = null);

        /// <summary>
        /// Revierte todos los asientos automáticos activos del origen (excepto reversos).
        /// </summary>
        Task<IReadOnlyList<int>> RevertirAsientosOrigenAsync(
            int idEmpresa,
            string origenModulo,
            int origenReferenciaId,
            int idUsuario,
            string? motivo = null);
    }
}
