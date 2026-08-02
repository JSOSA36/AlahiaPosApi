using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface ITesoreriaConciliacionService
    {
        Task<TesoreriaConciliacion> CrearConciliacionAsync(CrearConciliacionDto dto);
        Task<TesoreriaConciliacion?> GetByIdAsync(int idTesoreriaConciliacion, int idEmpresa);
        Task<ConciliacionWorkspaceDto> GetWorkspaceAsync(int idTesoreriaConciliacion, int idEmpresa);
        Task<TesoreriaExtractoImport> AdjuntarExtractoAsync(AdjuntarExtractoAConciliacionDto dto);
        Task<TesoreriaExtractoImport> ImportarYAdjuntarAsync(int idTesoreriaConciliacion, ImportarExtractoDto dto);
        Task<ConciliacionWorkspaceDto> EjecutarMatchingAsync(EjecutarMatchingConciliacionDto dto);
        Task<ResolverExtractoLineaResultadoDto> ResolverLineaAsync(ResolverLineaConciliacionDto dto);
        Task DeshacerMatchAsync(DeshacerMatchConciliacionDto dto);
        Task<IEnumerable<MovimientoFinancieroListadoDto>> BuscarCandidatosAsync(BuscarCandidatosMatchDto dto);
        Task<IEnumerable<MovimientoFinancieroListadoDto>> ListarMovimientosPendientesAsync(
            int idTesoreriaConciliacion,
            int idEmpresa);
        Task MarcarConciliadosAsync(MarcarConciliacionMovimientosDto dto);
        Task DesmarcarConciliadosAsync(MarcarConciliacionMovimientosDto dto);
        Task<int> RegistrarCargoInteresAsync(RegistrarCargoInteresDto dto);
        Task<TesoreriaConciliacion> CerrarConciliacionAsync(int idTesoreriaConciliacion, int idEmpresa, int idUsuario);
        Task<TesoreriaConciliacion> ReabrirConciliacionAsync(ReabrirConciliacionDto dto);
        Task<ReclasificarPagoResultadoDto> ReversarReclasificacionAsync(ReversarReclasificacionPagoDto dto);
        Task<IEnumerable<ConciliacionResumenDto>> GetHistorialAsync(int idEmpresa, int? idCuentaFinanciera = null);
        Task<IEnumerable<MovimientoFinancieroListadoDto>> GetPendientesConciliacionAsync(
            int idEmpresa,
            int idCuentaFinanciera,
            DateTime? hasta = null);
    }
}
