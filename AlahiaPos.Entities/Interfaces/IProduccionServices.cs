using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Adapter POS → Centro de Producción. Publica snapshot post-commit; no bloquea ventas.
    /// </summary>
    public interface IProduccionPosAdapter
    {
        /// <summary>
        /// Si es orden (IdTipoDocumentos=10) y el módulo está activo, publica evento de creación o actualización.
        /// origenIdAnterior: cuando el POS recrea la factura al editar (delete+insert).
        /// No-op y sin excepción hacia el caller si falla o no aplica.
        /// </summary>
        Task PublicarOrdenSiAplicaAsync(FacturaHeaders header, int? origenIdAnterior = null);
    }

    public interface IProduccionTrabajoService
    {
        Task<ProduccionTrabajoDto?> CrearDesdeEventoAsync(ProduccionTrabajoSolicitadoEvent solicitud);
        Task<ProduccionTrabajoDto?> ActualizarDesdeEventoAsync(ProduccionTrabajoActualizadoEvent solicitud);
        Task<List<ProduccionTrabajoDto>> ListarActivosAsync(int idEmpresa, string? tipoTrabajoCodigo = null);
        /// <summary>Último estado de producción por OrigenId (incluye fuera del tablero).</summary>
        Task<List<ProduccionEstadoOrigenDto>> ObtenerEstadosPorOrigenAsync(
            int idEmpresa, string origenTipo, IEnumerable<int> origenIds);
        Task<ProduccionTrabajoDto?> ObtenerAsync(int idEmpresa, int idTrabajo);
        Task<ProduccionTrabajoDto> TransicionarAsync(int idEmpresa, int idTrabajo, ProduccionTransicionRequest request);
        Task<ProduccionTrabajoDto> CancelarAsync(int idEmpresa, int idTrabajo, ProduccionCancelarRequest request);
        Task<ProduccionTrabajoDto> CambiarPrioridadAsync(int idEmpresa, int idTrabajo, ProduccionPrioridadRequest request);
        Task<List<ProduccionHistorialDto>> ListarHistorialAsync(int idEmpresa, int idTrabajo);
        Task<ProduccionDashboardResumenDto> ObtenerDashboardAsync(int idEmpresa, string? tipoTrabajoCodigo = null);
    }

    public interface IProduccionConfiguracionService
    {
        Task<ProduccionConfiguracionDto?> ObtenerAsync(int idEmpresa);
        Task<ProduccionConfiguracionDto> ActualizarAsync(ProduccionConfiguracionDto dto);
        Task<bool> EstaActivoAsync(int idEmpresa);
    }

    public interface IProduccionFlujoService
    {
        Task<ProduccionFlujoDto?> ObtenerFlujoActivoAsync(int idEmpresa, string tipoTrabajoCodigo);
        Task<List<ProduccionFlujoDto>> ListarFlujosAsync(int idEmpresa);
    }
}
