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

        /// <summary>
        /// Reserva atómica via SQL UPDATE...OUTPUT.
        /// Segura bajo concurrencia (múltiples cajas simultáneas).
        /// </summary>
        Task<ReservaEcfResultado> ReservarSiguienteAsync(int idEmpresa, int tipoEcfDgii);

        Task<string?> PeekSiguienteAsync(int idEmpresa, int tipoEcfDgii);
        Task<SecuenciaAlertaDto> ValidarDisponibilidadAsync(int idEmpresa, int tipoEcfDgii);
        Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerDisponiblesAsync(int idEmpresa);
    }
}
