using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission.Providers.Dgii;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Options;

namespace Alahia.eCF.Api.Services
{
    /// <summary>
    /// Facade "Emitir e-CF": XML → Firma → Transmission Engine.
    /// </summary>
    public class ReceiptOrchestrator
    {
        public const decimal UmbralRfceMontoTotal = Ecf32Definition.UmbralRfceMontoTotal;

        private readonly ITransmissionEngine _transmission;
        private readonly DgiiXmlBuilder _xmlBuilder;
        private readonly DgiiRfceBuilder _rfceBuilder;
        private readonly DgiiCertificadoResolver _certs;
        private readonly IFiscalGateway _gateway;
        private readonly IFiscalDocumentoValidator _validator;
        private readonly DgiiAuthService _auth;
        private readonly DgiiDirectoSettings _settings;
        private readonly ILogger<ReceiptOrchestrator> _logger;

        public ReceiptOrchestrator(
            ITransmissionEngine transmission,
            DgiiXmlBuilder xmlBuilder,
            DgiiRfceBuilder rfceBuilder,
            DgiiCertificadoResolver certs,
            IFiscalGateway gateway,
            IFiscalDocumentoValidator validator,
            DgiiAuthService auth,
            IOptions<DgiiDirectoSettings> settings,
            ILogger<ReceiptOrchestrator> logger)
        {
            _transmission = transmission;
            _xmlBuilder = xmlBuilder;
            _rfceBuilder = rfceBuilder;
            _certs = certs;
            _gateway = gateway;
            _validator = validator;
            _auth = auth;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<PgTrackIdResponse> EnviarAsync(PgDgiiDocumentDto documento, CancellationToken ct)
        {
            var fiscal = PgReceiptMapper.ToFiscal(documento);
            AsegurarValidacion(fiscal);
            var canal = ResolverCanal(fiscal.Encabezado.TipoEcf, fiscal.Encabezado.MontoTotal);

            _logger.LogInformation(
                "Emitir e-CF: {Encf} tipo={Tipo} monto={Monto} canal={Canal} ambiente={Ambiente}",
                fiscal.Encabezado.Encf,
                fiscal.Encabezado.TipoEcf,
                fiscal.Encabezado.MontoTotal,
                canal,
                _settings.AmbientePath);

            var package = await BuildPackageAsync(fiscal, canal, ct);
            var accept = await _transmission.SubmitAsync(package, ct);
            return ToPgResponse(accept, fiscal, canal);
        }

        public async Task<PgTrackIdResponse> EnviarRfceAsync(PgDgiiDocumentDto documento, CancellationToken ct)
        {
            var fiscal = PgReceiptMapper.ToFiscal(documento);
            AsegurarValidacion(fiscal);
            if (fiscal.Encabezado.TipoEcf != 32)
                throw new InvalidOperationException("RFCE solo aplica a TipoeCF=32.");
            if (fiscal.Encabezado.MontoTotal >= UmbralRfceMontoTotal)
                throw new InvalidOperationException(
                    $"MontoTotal {fiscal.Encabezado.MontoTotal} >= {UmbralRfceMontoTotal}; usar canal e-CF individual.");

            var package = await BuildPackageAsync(fiscal, "RFCE", ct);
            var accept = await _transmission.SubmitAsync(package, ct);
            return ToPgResponse(accept, fiscal, "RFCE");
        }

        private void AsegurarValidacion(FiscalDocumentoElectronico fiscal)
        {
            var validacion = _validator.Validar(fiscal);
            if (validacion.Ok) return;
            _logger.LogWarning("Validación FE rechazada: {Codigo} {Mensaje}", validacion.Codigo, validacion.Mensaje);
            throw new FiscalValidationException(validacion.Mensaje ?? "Documento fiscal inválido", validacion.Codigo);
        }

        public async Task<PgTrackIdResponse> ConsultarAsync(string trackId, CancellationToken ct)
        {
            var result = await _gateway.ConsultarEstadoAsync(trackId, ct);
            return PgReceiptMapper.ToPgResponse(result);
        }

        public async Task<PgTrackIdResponse?> ConsultarJobAsync(Guid jobId, CancellationToken ct)
        {
            var job = await _transmission.GetJobAsync(jobId, ct);
            if (job == null) return null;
            return ToPgResponse(new TransmissionAcceptResult
            {
                JobId = job.JobId,
                State = job.State,
                ProviderReceiptId = job.ProviderReceiptId,
                ProviderStatusLabel = job.ProviderStatusLabel,
                Messages = string.IsNullOrEmpty(job.LastError) ? new() : new() { job.LastError },
                AcceptedForProcessing = true,
                Extras = new Dictionary<string, string>(job.ResultExtras)
            }, null, job.Package.ProviderChannel);
        }

        public async Task<PgTrackIdResponse> ReenviarJobAsync(Guid jobId, string requestedBy, CancellationToken ct)
        {
            var accept = await _transmission.RetryManualAsync(jobId, requestedBy, ct);
            return ToPgResponse(accept, null, accept.Extras.GetValueOrDefault("canal"));
        }

        public Task<bool> HealthAsync(CancellationToken ct) => _gateway.VerificarConexionAsync(ct);

        public Task<string> SemillaAsync(CancellationToken ct) => _auth.ObtenerSemillaAsync(ct);

        public static string ResolverCanal(int tipoeCf, decimal montoTotal)
            => tipoeCf == 32 && montoTotal < UmbralRfceMontoTotal ? "RFCE" : "ECF";

        public object Info() => new
        {
            provider = TransmissionProviderCodes.Dgii,
            transmissionEngine = true,
            ambiente = _settings.AmbientePath,
            authBase = _settings.AuthBaseUrl,
            recepcionEcf = _settings.RecepcionBaseUrl,
            recepcionRfce = _settings.RecepcionFcBaseUrl,
            umbralRfce = UmbralRfceMontoTotal,
            reglaE32 = "TipoeCF=32 y MontoTotal < 250000 → RFCE (B2C); si no → ECF individual"
        };

        private async Task<TransmissionPackage> BuildPackageAsync(
            FiscalDocumentoElectronico fiscal,
            string canal,
            CancellationToken ct)
        {
            var fechaFirma = DateTime.Now;
            var material = await _certs.ResolveAsync(fiscal.IdEmpresa, ct);
            string payload;
            string? securityCode = null;
            string fileName;

            if (canal == "RFCE")
            {
                var xmlEcf = _xmlBuilder.Build(fiscal, fechaFirma);
                var xmlEcfFirmado = _certs.Firmar(xmlEcf, material);
                var rfce = _rfceBuilder.BuildFromSignedEcf(xmlEcfFirmado, out var codigo);
                payload = _certs.Firmar(rfce, material);
                securityCode = codigo;
                fileName = DgiiRfceBuilder.NombreArchivo(fiscal.Encabezado.RncEmisor, fiscal.Encabezado.Encf);
            }
            else
            {
                var xml = _xmlBuilder.Build(fiscal, fechaFirma);
                payload = _certs.Firmar(xml, material);
                securityCode = XmlSigner.ExtractCodigoSeguridad(payload);
                fileName = DgiiXmlBuilder.NombreArchivo(fiscal.Encabezado.RncEmisor, fiscal.Encabezado.Encf);
            }

            var taxpayer = DgiiXmlBuilder.NormalizarRnc(fiscal.Encabezado.RncEmisor);
            var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DgiiTransmissionProvider.MetaFileName] = fileName,
                [DgiiTransmissionProvider.MetaIdEmpresa] = fiscal.IdEmpresa.ToString(),
                ["result.canal"] = canal,
                ["result.fechaFirma"] = fechaFirma.ToString("o")
            };
            if (!string.IsNullOrEmpty(securityCode))
            {
                meta["result.securityCode"] = securityCode;
                meta["result.qr"] = DgiiDirectoMapper.BuildQrUrl(fiscal, fechaFirma, securityCode, _settings);
            }

            return new TransmissionPackage
            {
                ProviderCode = TransmissionProviderCodes.Dgii,
                TaxpayerId = taxpayer,
                DocumentId = fiscal.Encabezado.Encf.Trim().ToUpperInvariant(),
                DocumentTypeHint = fiscal.Encabezado.TipoEcf.ToString(),
                ProviderChannel = canal,
                Payload = payload,
                PayloadContentType = "application/xml",
                PayloadSha256 = TransmissionPackage.ComputeSha256(payload),
                IssuedAtUtc = fechaFirma.ToUniversalTime(),
                TenantId = fiscal.IdEmpresa,
                SourceDocumentId = fiscal.IdDocumentoInterno > 0 ? fiscal.IdDocumentoInterno.ToString() : null,
                ProviderMetadata = meta
            };
        }

        private static PgTrackIdResponse ToPgResponse(
            TransmissionAcceptResult accept,
            FiscalDocumentoElectronico? fiscal,
            string? canal)
        {
            var estadoErp = MapEstadoErp(accept.State, accept.ProviderStatusLabel);
            var codigo = accept.State switch
            {
                TransmissionJobStates.Accepted or TransmissionJobStates.AcceptedConditional => "1",
                TransmissionJobStates.Rejected => "2",
                TransmissionJobStates.PendingProvider => "1",
                _ => accept.State is TransmissionJobStates.TransientPendingRetry
                    or TransmissionJobStates.Queued
                    or TransmissionJobStates.Sending
                    ? "0"
                    : "2"
            };

            accept.Extras.TryGetValue("securityCode", out var sec);
            accept.Extras.TryGetValue("qr", out var qr);
            accept.Extras.TryGetValue("fechaFirma", out var ff);
            accept.Extras.TryGetValue("canal", out var canalExtra);

            return new PgTrackIdResponse
            {
                trackId = accept.ProviderReceiptId,
                codigo = codigo,
                estado = estadoErp,
                encf = fiscal?.Encabezado.Encf,
                rnc = fiscal != null ? DgiiXmlBuilder.NormalizarRnc(fiscal.Encabezado.RncEmisor) : null,
                secuenciaUtilizada = accept.State is TransmissionJobStates.Accepted
                    or TransmissionJobStates.AcceptedConditional
                    or TransmissionJobStates.PendingProvider,
                mensajes = accept.Messages.Select(m => new PgMensajeDto { valor = m }).ToList(),
                securityCode = sec,
                qr = qr,
                fechaFirma = ff,
                canal = canal ?? canalExtra,
                transmissionJobId = accept.JobId.ToString("D"),
                transmissionState = accept.State
            };
        }

        private static string MapEstadoErp(string engineState, string? providerLabel)
        {
            return engineState switch
            {
                TransmissionJobStates.Accepted => "Aceptado",
                TransmissionJobStates.AcceptedConditional => "AceptadoCondicional",
                TransmissionJobStates.Rejected => "Rechazado",
                TransmissionJobStates.PendingProvider => string.IsNullOrWhiteSpace(providerLabel) ? "EnProceso" : providerLabel!,
                TransmissionJobStates.TransientPendingRetry => "PendienteEnvio",
                TransmissionJobStates.Queued => "PendienteEnvio",
                TransmissionJobStates.Sending => "EnProceso",
                TransmissionJobStates.DeadlineExceeded => "VencidoSinEnvio",
                TransmissionJobStates.RequiresIntervention => "RequiereIntervencion",
                _ => engineState
            };
        }
    }
}
