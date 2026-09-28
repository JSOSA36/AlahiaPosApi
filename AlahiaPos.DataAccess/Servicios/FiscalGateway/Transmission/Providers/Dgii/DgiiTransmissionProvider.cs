using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission.Providers.Dgii
{
    /// <summary>
    /// Provider DGII: solo auth/send/query. No cola ni políticas del Engine.
    /// </summary>
    public sealed class DgiiTransmissionProvider : ITransmissionProvider
    {
        public const string MetaFileName = "fileName";
        public const string MetaIdEmpresa = "idEmpresa";
        public const string MetaAmbiente = "dgiiAmbiente";

        private readonly DgiiRecepcionClient _recepcion;
        private readonly DgiiTransmissionProviderOptions _options;
        private readonly ILogger<DgiiTransmissionProvider> _logger;

        public string ProviderCode => TransmissionProviderCodes.Dgii;
        public bool SupportsStatusQuery => _options.StatusQuery.Enabled;

        public DgiiTransmissionProvider(
            DgiiRecepcionClient recepcion,
            IOptions<TransmissionProvidersOptions> providers,
            ILogger<DgiiTransmissionProvider> logger)
        {
            _recepcion = recepcion;
            _options = providers.Value.DGII;
            _logger = logger;
        }

        public async Task<ProviderSendResult> SendAsync(TransmissionPackage package, CancellationToken ct)
        {
            if (!_options.Enabled)
                return Fail(ProviderOutcome.RejectedBusiness, "DGII provider deshabilitado");

            var fileName = package.ProviderMetadata.GetValueOrDefault(MetaFileName)
                           ?? $"{package.TaxpayerId}{package.DocumentId}.xml";
            var idEmpresa = 0;
            if (package.ProviderMetadata.TryGetValue(MetaIdEmpresa, out var idStr))
                int.TryParse(idStr, out idEmpresa);
            package.ProviderMetadata.TryGetValue(MetaAmbiente, out var ambienteMeta);

            var sw = Stopwatch.StartNew();
            using var _ = DgiiAmbienteContext.Push(ambienteMeta);
            using var emp = DgiiEmpresaContext.Push(idEmpresa);
            try
            {
                DgiiHttpResultado resp;
                var channel = (package.ProviderChannel ?? "ECF").Trim().ToUpperInvariant();
                if (channel == "RFCE")
                    resp = await _recepcion.EnviarRfceAsync(package.Payload, fileName, idEmpresa, ct);
                else
                    resp = await _recepcion.EnviarEcfAsync(package.Payload, fileName, idEmpresa, ct);

                sw.Stop();
                return MapSend(resp, channel);
            }
            catch (TaskCanceledException)
            {
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.TransientFailure,
                    Messages = { "Timeout de comunicación con DGII" },
                    ProviderStatusCode = "TIMEOUT"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "DGII HTTP transient {DocumentId}", package.DocumentId);
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.TransientFailure,
                    Messages = { ex.Message },
                    ProviderStatusCode = "HTTP_ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DGII send error {DocumentId}", package.DocumentId);
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.Ambiguous,
                    Messages = { ex.Message },
                    ProviderStatusCode = "PROVIDER_ERROR"
                };
            }
        }

        public async Task<ProviderStatusResult> QueryStatusAsync(
            TransmissionPackage package,
            string providerReceiptId,
            CancellationToken ct)
        {
            var idEmpresa = 0;
            if (package.ProviderMetadata.TryGetValue(MetaIdEmpresa, out var idStr))
                int.TryParse(idStr, out idEmpresa);
            package.ProviderMetadata.TryGetValue(MetaAmbiente, out var ambienteMeta);

            using var _ = DgiiAmbienteContext.Push(ambienteMeta);
            using var emp = DgiiEmpresaContext.Push(idEmpresa);
            try
            {
                var resp = await _recepcion.ConsultarEstadoAsync(providerReceiptId, idEmpresa, ct);
                return MapStatus(resp, providerReceiptId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DGII query {ReceiptId}", providerReceiptId);
                return new ProviderStatusResult
                {
                    Outcome = ProviderStatusOutcome.Unknown,
                    ProviderReceiptId = providerReceiptId,
                    Messages = { ex.Message }
                };
            }
        }

        private static ProviderSendResult MapSend(DgiiHttpResultado resp, string channel)
        {
            var trackOk = !string.IsNullOrWhiteSpace(resp.TrackId)
                && !string.Equals(resp.TrackId, "00000000-0000-0000-0000-000000000000", StringComparison.OrdinalIgnoreCase);

            var label = (resp.Estado ?? "").Trim();
            var messages = resp.Mensajes.Count > 0
                ? resp.Mensajes
                : (string.IsNullOrWhiteSpace(resp.Mensaje) ? new List<string>() : new List<string> { resp.Mensaje });

            // RFCE suele responder Aceptado de inmediato sin trackId
            if (channel == "RFCE")
            {
                if (label.Equals("Aceptado", StringComparison.OrdinalIgnoreCase) ||
                    label.Equals("AceptadoCondicional", StringComparison.OrdinalIgnoreCase) ||
                    resp.Codigo == "1")
                {
                    return new ProviderSendResult
                    {
                        Outcome = label.Contains("Condicional", StringComparison.OrdinalIgnoreCase)
                            ? ProviderOutcome.AcceptedImmediate
                            : ProviderOutcome.AcceptedImmediate,
                        ProviderReceiptId = trackOk ? resp.TrackId : null,
                        ProviderStatusCode = resp.Codigo,
                        ProviderStatusLabel = string.IsNullOrEmpty(label) ? "Aceptado" : label,
                        Messages = messages,
                        RawResponse = Trunc(resp.Body),
                        HttpStatusCode = resp.StatusCode
                    };
                }

                if (YaConsumidoPorDgii(messages, resp.Mensaje, resp.Body))
                {
                    return new ProviderSendResult
                    {
                        Outcome = ProviderOutcome.AcceptedImmediate,
                        ProviderReceiptId = trackOk ? resp.TrackId : null,
                        ProviderStatusCode = "1",
                        ProviderStatusLabel = "Aceptado",
                        Messages = messages.Count > 0 ? messages : new List<string> { "RFCE ya recibido por DGII (e-NCF y código de seguridad)." },
                        RawResponse = Trunc(resp.Body),
                        HttpStatusCode = resp.StatusCode
                    };
                }

                if (!resp.Ok || label.Equals("Rechazado", StringComparison.OrdinalIgnoreCase))
                {
                    var transient = resp.StatusCode >= 500 || resp.StatusCode == 0;
                    return new ProviderSendResult
                    {
                        Outcome = transient ? ProviderOutcome.TransientFailure : ProviderOutcome.RejectedBusiness,
                        ProviderStatusCode = resp.Codigo ?? $"HTTP_{resp.StatusCode}",
                        ProviderStatusLabel = string.IsNullOrEmpty(label) ? "Rechazado" : label,
                        Messages = messages.Count > 0 ? messages : new List<string> { Trunc(resp.Body) },
                        RawResponse = Trunc(resp.Body),
                        HttpStatusCode = resp.StatusCode
                    };
                }
            }

            if (trackOk && resp.Ok)
            {
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.AcceptedPending,
                    ProviderReceiptId = resp.TrackId,
                    ProviderStatusCode = resp.Codigo,
                    ProviderStatusLabel = string.IsNullOrEmpty(label) ? "EnProceso" : label,
                    Messages = messages,
                    RawResponse = Trunc(resp.Body),
                    HttpStatusCode = resp.StatusCode
                };
            }

            if (label.Equals("Rechazado", StringComparison.OrdinalIgnoreCase))
            {
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.RejectedBusiness,
                    ProviderStatusCode = resp.Codigo,
                    ProviderStatusLabel = label,
                    Messages = messages.Count > 0 ? messages : new List<string> { Trunc(resp.Body) },
                    RawResponse = Trunc(resp.Body),
                    HttpStatusCode = resp.StatusCode
                };
            }

            if (resp.StatusCode >= 500 || resp.StatusCode == 408 || !resp.Ok)
            {
                return new ProviderSendResult
                {
                    Outcome = ProviderOutcome.TransientFailure,
                    ProviderStatusCode = resp.Codigo ?? $"HTTP_{resp.StatusCode}",
                    ProviderStatusLabel = label,
                    Messages = messages.Count > 0 ? messages : new List<string> { Trunc(resp.Body) },
                    RawResponse = Trunc(resp.Body),
                    HttpStatusCode = resp.StatusCode
                };
            }

            return new ProviderSendResult
            {
                Outcome = ProviderOutcome.Ambiguous,
                ProviderStatusCode = resp.Codigo,
                ProviderStatusLabel = label,
                Messages = messages.Count > 0 ? messages : new List<string> { Trunc(resp.Body) },
                RawResponse = Trunc(resp.Body),
                HttpStatusCode = resp.StatusCode
            };
        }

        private static ProviderStatusResult MapStatus(DgiiHttpResultado resp, string receiptId)
        {
            var label = (resp.Estado ?? "").Trim();
            var messages = resp.Mensajes.Count > 0 ? resp.Mensajes : new List<string>();

            ProviderStatusOutcome outcome;
            if (label.Equals("Aceptado", StringComparison.OrdinalIgnoreCase))
                outcome = ProviderStatusOutcome.Accepted;
            else if (label.Contains("Condicional", StringComparison.OrdinalIgnoreCase))
                outcome = ProviderStatusOutcome.AcceptedConditional;
            else if (label.Equals("Rechazado", StringComparison.OrdinalIgnoreCase))
                outcome = ProviderStatusOutcome.Rejected;
            else if (label.Contains("No encontrado", StringComparison.OrdinalIgnoreCase) || resp.Codigo == "0")
                outcome = ProviderStatusOutcome.NotFound;
            else if (label.Equals("EnProceso", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(label))
                outcome = ProviderStatusOutcome.Pending;
            else
                outcome = ProviderStatusOutcome.Unknown;

            return new ProviderStatusResult
            {
                Outcome = outcome,
                ProviderReceiptId = resp.TrackId ?? receiptId,
                ProviderStatusCode = resp.Codigo,
                ProviderStatusLabel = string.IsNullOrEmpty(label) ? outcome.ToString() : label,
                Messages = messages,
                RawResponse = Trunc(resp.Body)
            };
        }

        private static ProviderSendResult Fail(ProviderOutcome o, string msg) => new()
        {
            Outcome = o,
            Messages = { msg }
        };

        private static bool YaConsumidoPorDgii(IReadOnlyList<string> messages, string? mensaje, string? body)
        {
            static bool Hit(string? t)
            {
                if (string.IsNullOrWhiteSpace(t)) return false;
                t = t.ToLowerInvariant();
                return t.Contains("ya han sido utilizados")
                    || t.Contains("utilizados previamente")
                    || t.Contains("ya ha sido utilizado")
                    || t.Contains("secuencia ya");
            }

            if (Hit(mensaje) || Hit(body)) return true;
            foreach (var m in messages)
            {
                if (Hit(m)) return true;
            }
            return false;
        }

        private static string Trunc(string? s, int max = 800)
            => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max]);
    }
}
