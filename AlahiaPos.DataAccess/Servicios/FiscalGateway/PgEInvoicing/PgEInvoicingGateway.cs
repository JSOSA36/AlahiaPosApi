using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing
{
    /// <summary>
    /// Obsoleto: use <see cref="IFiscalGateway"/> vía DI.
    /// </summary>
    [Obsolete("Usar IFiscalGateway (HttpReceiptFiscalGateway). No registrar esta clase en el ERP.")]
    public sealed class PgEInvoicingGateway : IFiscalGateway
    {
        private readonly IFiscalGateway _inner;

        public PgEInvoicingGateway(IFiscalGateway inner) => _inner = inner;

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
