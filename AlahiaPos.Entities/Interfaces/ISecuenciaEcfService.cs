using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// CRUD y reserva atómica de secuencias e-CF.
    /// Propiedad exclusiva del módulo de Facturación Electrónica.
    /// </summary>
    public interface ISecuenciaEcfService
    {
        Task<IEnumerable<SecuenciaEcfDto>> GetAllAsync(int idEmpresa);
        Task<SecuenciaEcfDto?> GetByIdAsync(int id);
        Task<SecuenciaEcfDto> CreateAsync(SecuenciaEcfCreateDto dto);
        Task UpdateAsync(int id, SecuenciaEcfUpdateDto dto);
        Task DesactivarAsync(int id);

        Task<SecuenciaEcfAsignacionDto> AsignarRangoAsync(int idSecuencia, SecuenciaEcfAsignarDto dto);
        Task ActualizarAsignacionAsync(int idAsignacion, SecuenciaEcfAsignarDto dto);
        Task DesactivarAsignacionAsync(int idAsignacion);

        /// <summary>
        /// Reserva atómica via SQL UPDATE...OUTPUT.
        /// Segura bajo concurrencia (múltiples cajas simultáneas).
        /// </summary>
        Task<ReservaEcfResultado> ReservarSiguienteAsync(int idEmpresa, int tipoEcfDgii, int? idSucursal = null);

        Task<string?> PeekSiguienteAsync(int idEmpresa, int tipoEcfDgii, int? idSucursal = null);
        Task<SecuenciaAlertaDto> ValidarDisponibilidadAsync(int idEmpresa, int tipoEcfDgii, int? idSucursal = null);
        Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerDisponiblesAsync(int idEmpresa, int? idSucursal = null);
        Task<SecuenciaECF?> ObtenerActivaAsync(int idEmpresa, int tipoEcfDgii, int? idSucursal = null);
    }
}
