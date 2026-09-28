using AlahiaPos.Entities.Dto.Fiscal;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Punto de entrada transversal para todos los módulos del ERP.
    /// Ningún módulo comercial accede directamente a secuencias,
    /// Gateway, proveedor, DGII ni estados del documento electrónico.
    /// </summary>
    public interface IFacturacionElectronicaService
    {
        /// <summary>
        /// Reserva e-NCF, crea ECFEncabezado, congela fotografía fiscal
        /// y encola el envío asíncrono al Gateway.
        /// Retorna inmediatamente sin esperar respuesta de DGII.
        /// </summary>
        Task<EmisionEcfResultado> EmitirDocumentoAsync(EmisionEcfRequest request);

        /// <summary>
        /// Reserva e-NCF, crea ECFEncabezado, envía al Gateway de forma síncrona
        /// y retorna resultado completo con QR, TrackId y estado DGII.
        /// Uso: POS preview, donde se necesita respuesta inmediata.
        /// </summary>
        Task<EmisionEcfResultadoCompleto> EmitirYEnviarAsync(EmisionEcfRequest request);

        /// <summary>
        /// Retorna las secuencias e-CF disponibles para una empresa.
        /// Para poblar selectores en POS, Facturación, NC, etc.
        /// </summary>
        Task<IReadOnlyList<SecuenciaEcfDisponibleDto>> ObtenerSecuenciasDisponiblesAsync(
            int idEmpresa, int? idSucursal = null);

        /// <summary>
        /// Mismo envío que POS/NCF: alinea definiciones, valida y entrega al Gateway.
        /// No reserva secuencia: el e-NCF ya viene en el documento (CerteCF / reintento).
        /// </summary>
        Task<FiscalEnvioResultado> EnviarDocumentoFiscalAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default);

        Task<FiscalConsultaResultado> ConsultarEstadoDgiiAsync(
            string trackId,
            int idEmpresa = 0,
            CancellationToken ct = default);
    }
}
