using AlahiaPos.DataAccess.Servicios.FiscalGateway;
using AlahiaPos.Entities.Dto.Fiscal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// El ERP no habla con DGII: reenvía ACECF a Alahia.eCF.Api (endpoint aparte de /api/Receipt).
    /// </summary>
    public sealed class CertecfAcecfHttpSender : ICertecfAcecfSender
    {
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _http;
        private readonly FiscalGatewayOptions _opts;
        private readonly ILogger<CertecfAcecfHttpSender> _logger;

        public CertecfAcecfHttpSender(
            IHttpClientFactory http,
            IOptions<FiscalGatewayOptions> opts,
            ILogger<CertecfAcecfHttpSender> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task<FiscalEnvioResultado> EnviarAsync(AcecfDocumento documento, CancellationToken ct = default)
        {
            var baseUrl = (_opts.BaseUrl ?? "").Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return FiscalEnvioResultado.Error("CONFIG", "FiscalGateway:BaseUrl no configurado.");

            try
            {
                var client = _http.CreateClient(nameof(CertecfAcecfHttpSender));
                client.Timeout = TimeSpan.FromSeconds(_opts.TimeoutSeconds > 0 ? _opts.TimeoutSeconds : 60);
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/CertecfAcecf")
                {
                    Content = new StringContent(JsonSerializer.Serialize(documento, Json), Encoding.UTF8, "application/json")
                };
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(_opts.ApiKey))
                    req.Headers.TryAddWithoutValidation("X-Api-Key", _opts.ApiKey);

                using var res = await client.SendAsync(req, ct);
                var body = await res.Content.ReadAsStringAsync(ct);
                var parsed = JsonSerializer.Deserialize<FiscalEnvioResultado>(body, Json);
                if (parsed != null && (parsed.Exitoso || !string.IsNullOrWhiteSpace(parsed.Estado)))
                    return parsed;
                if (!res.IsSuccessStatusCode)
                    return FiscalEnvioResultado.Error($"HTTP_{(int)res.StatusCode}", body);
                return parsed ?? FiscalEnvioResultado.Error("ACECF", body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ACECF HTTP {Encf}", documento.Encf);
                return FiscalEnvioResultado.Error("HTTP_ERROR", ex.Message);
            }
        }
    }
}
