using AlahiaPos.Entities.Dto.Fiscal;
using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Puerto principal del Gateway Fiscal.
    /// El Motor Fiscal SOLO conoce esta interfaz.
    /// Nunca ve modelos del proveedor, API keys, HTTP ni XML.
    /// </summary>
    public interface IFiscalGateway
    {
        Task<FiscalEnvioResultado> EnviarDocumentoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default);

        Task<FiscalConsultaResultado> ConsultarEstadoAsync(
            string trackId,
            int idEmpresa = 0,
            CancellationToken ct = default);

        Task<bool> VerificarConexionAsync(CancellationToken ct = default);
    }
}
