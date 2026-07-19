using AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Http
{
    /// <summary>
    /// Adaptador HTTP del Gateway Fiscal hacia cualquier proveedor que exponga
    /// el contrato wire <c>/api/Receipt</c> (PG.eInvoicing, Alahia.eCF.Api, u otro compatible).
    /// El ERP inyecta <see cref="IFiscalGateway"/>; nunca referencia esta clase.
    /// </summary>
    public sealed class HttpReceiptFiscalGateway : IFiscalGateway
    {
        private readonly HttpClient _http;
        private readonly FiscalGatewayOptions _settings;
        private readonly ILogger<HttpReceiptFiscalGateway> _logger;

        private static readonly JsonSerializerOptions JsonWrite = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions JsonRead = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public HttpReceiptFiscalGateway(
            HttpClient http,
            IOptions<FiscalGatewayOptions> settings,
            ILogger<HttpReceiptFiscalGateway> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new InvalidOperationException(
                    "FiscalGateway:BaseUrl no configurado. Defina BaseUrl y ApiKey en appsettings (sección FiscalGateway).");

            _http.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                _http.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _settings.ApiKey);
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _http.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 30);
        }

        public async Task<FiscalEnvioResultado> EnviarDocumentoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            try
            {
                var wire = PgEInvoicingMapper.ToProviderModel(documento);
                var json = JsonSerializer.Serialize(wire, JsonWrite);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Gateway: enviando e-CF {Encf} tipo {Tipo} → {Url}",
                    documento.Encabezado.Encf, documento.Encabezado.TipoEcf, _settings.BaseUrl);

                var response = await _http.PostAsync("api/Receipt", content, ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Gateway: HTTP {Status} para {Encf}: {Body}",
                        (int)response.StatusCode, documento.Encabezado.Encf, Truncar(responseBody, 500));

                    return FiscalEnvioResultado.Error(
                        $"HTTP_{(int)response.StatusCode}",
                        $"Proveedor retornó {response.StatusCode}: {Truncar(responseBody, 500)}");
                }

                var wireResp = JsonSerializer.Deserialize<PgTrackIdResponse>(responseBody, JsonRead);
                return PgEInvoicingMapper.ToEnvioResultado(wireResp, documento.Encabezado.TipoEcf);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Gateway: timeout al enviar {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("TIMEOUT", "Timeout de comunicación con el proveedor");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Gateway: error HTTP al enviar {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("HTTP_ERROR", $"Error de comunicación: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway: error inesperado al enviar {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("GATEWAY_ERROR", ex.Message);
            }
        }

        public async Task<FiscalConsultaResultado> ConsultarEstadoAsync(
            string trackId,
            CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync($"api/Receipt/{trackId}", ct);
                var responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return new FiscalConsultaResultado
                    {
                        TrackId = trackId,
                        Estado = "Error",
                        CodigoError = $"HTTP_{(int)response.StatusCode}",
                        Mensajes = new() { Truncar(responseBody, 500) }
                    };
                }

                var wireResp = JsonSerializer.Deserialize<PgTrackIdResponse>(responseBody, JsonRead);
                return PgEInvoicingMapper.ToConsultaResultado(wireResp, trackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gateway: error al consultar TrackId {TrackId}", trackId);
                return new FiscalConsultaResultado
                {
                    TrackId = trackId,
                    Estado = "Error",
                    CodigoError = "GATEWAY_ERROR",
                    Mensajes = new() { ex.Message }
                };
            }
        }

        public async Task<bool> VerificarConexionAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync("api/Receipt", ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static string Truncar(string? s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";
    }
}
