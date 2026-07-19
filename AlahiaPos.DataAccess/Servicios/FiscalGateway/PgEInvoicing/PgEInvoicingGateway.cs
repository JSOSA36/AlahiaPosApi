using AlahiaPos.DataAccess.Servicios.FiscalGateway.Http;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing
{
    /// <summary>
    /// Obsoleto: use <see cref="HttpReceiptFiscalGateway"/> vía <see cref="IFiscalGateway"/>.
    /// Se mantiene temporalmente por compatibilidad de referencias internas.
    /// </summary>
    [Obsolete("Usar IFiscalGateway (HttpReceiptFiscalGateway). No registrar esta clase en el ERP.")]
    public sealed class PgEInvoicingGateway : IFiscalGateway
    {
        private readonly HttpReceiptFiscalGateway _inner;

        public PgEInvoicingGateway(
            HttpClient http,
            IOptions<FiscalGatewayOptions> settings,
            ILogger<HttpReceiptFiscalGateway> logger)
        {
            _inner = new HttpReceiptFiscalGateway(http, settings, logger);
        }

        public Task<FiscalEnvioResultado> EnviarDocumentoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
            => _inner.EnviarDocumentoAsync(documento, ct);

        public Task<FiscalConsultaResultado> ConsultarEstadoAsync(
            string trackId,
            CancellationToken ct = default)
            => _inner.ConsultarEstadoAsync(trackId, ct);

        public Task<bool> VerificarConexionAsync(CancellationToken ct = default)
            => _inner.VerificarConexionAsync(ct);
    }
}
