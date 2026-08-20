using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IEmpleadoLaboralService
    {
        Task<EmpleadoLaboral?> GetLaboralAsync(int idEmpresa, int idEmpleados);
        Task<EmpleadoLaboralVistaDto?> GetLaboralVistaAsync(int idEmpresa, int idEmpleados);
        Task<EmpleadoLaboralVistaDto> UpsertLaboralAsync(EmpleadoLaboral laboral, string? motivoCambioSalario = null, int? idUsuario = null);
        Task SincronizarEmpleadosDelCargoAsync(int idEmpresa, int idCargo, int? idUsuario = null);
        Task<IReadOnlyList<EmpleadoSalarioHistorial>> GetHistorialSalarialAsync(int idEmpresa, int idEmpleados);
        Task<IReadOnlyList<NominaConcepto>> GetConceptosAsync(int idEmpresa);
        Task EnsureCatalogoBaseAsync(int idEmpresa);
        Task<NominaConcepto> UpsertConceptoAsync(NominaConcepto concepto);
        Task<IReadOnlyList<NominaConceptoAsignacion>> GetAsignacionesAsync(int idEmpresa, int idEmpleados);
        Task<NominaConceptoAsignacion> UpsertAsignacionAsync(NominaConceptoAsignacion asignacion);
        Task<IReadOnlyList<ConsumoColaboradorDto>> GetConsumoColaboradoresAsync(int idEmpresa);
    }
}
