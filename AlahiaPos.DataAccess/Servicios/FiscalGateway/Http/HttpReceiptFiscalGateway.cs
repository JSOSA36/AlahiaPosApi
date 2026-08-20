using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
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
    /// Resuelve BaseUrl/ApiKey por empresa (DGII_DIRECTO → appsettings; PROVEEDOR_EXTERNO → Empresas).
    /// </summary>
    public sealed class HttpReceiptFiscalGateway : IFiscalGateway
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly FiscalGatewayOptions _settings;
        private readonly IEmpresaFiscalGatewayResolver _resolver;
        private readonly AlahiaPosContext _ctx;
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
            IHttpClientFactory httpFactory,
            IOptions<FiscalGatewayOptions> settings,
            IEmpresaFiscalGatewayResolver resolver,
            AlahiaPosContext ctx,
            ILogger<HttpReceiptFiscalGateway> logger)
        {
            _httpFactory = httpFactory;
            _settings = settings.Value;
            _resolver = resolver;
            _ctx = ctx;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
                throw new InvalidOperationException(
                    "FiscalGateway:BaseUrl no configurado. Defina BaseUrl y ApiKey en appsettings (sección FiscalGateway) para modo DGII_DIRECTO.");
        }

        public async Task<FiscalEnvioResultado> EnviarDocumentoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            try
            {
                var ep = await ResolveEndpointAsync(documento.IdEmpresa, ct);
                var wire = PgEInvoicingMapper.ToProviderModel(documento);
                var json = JsonSerializer.Serialize(wire, JsonWrite);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var ambiente = (documento.AmbienteDgii ?? "").Trim();
                var url = $"{ep.BaseUrl}/api/Receipt";
                using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
                ApplyAuth(request, ep);
                if (ep.Modo == ProveedorFiscalHelper.DgiiDirecto && !string.IsNullOrWhiteSpace(ambiente))
                    request.Headers.TryAddWithoutValidation("X-Dgii-Ambiente", ambiente);

                _logger.LogInformation(
                    "Gateway: enviando e-CF {Encf} tipo {Tipo} modo={Modo} ambiente={Ambiente} → {Url}",
                    documento.Encabezado.Encf, documento.Encabezado.TipoEcf, ep.Modo, ambiente, ep.BaseUrl);

                using var response = await SendAsync(request, ep.TimeoutSeconds, ct);
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
                var resultado = PgEInvoicingMapper.ToEnvioResultado(wireResp, documento.Encabezado.TipoEcf);
                // Invoice devuelve qr como data:image; ECFEncabezado.UrlQR es NVARCHAR(500)
                // y la térmica necesita URL de ConsultaTimbre para PrintQRCode.
                if (resultado.Exitoso || !string.IsNullOrWhiteSpace(resultado.SecurityCode))
                {
                    resultado.UrlQR = EcfQrUrlHelper.ResolveForStorage(
                        resultado.UrlQR,
                        documento,
                        resultado.FechaFirma,
                        resultado.SecurityCode);
                }
                return resultado;
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
                var idEmpresa = await _ctx.ECFEncabezados.AsNoTracking()
                    .Where(e => e.TrackId == trackId)
                    .Select(e => (int?)e.IdEmpresa)
                    .FirstOrDefaultAsync(ct);

                var ep = idEmpresa is > 0
                    ? await _resolver.ResolveAsync(idEmpresa.Value, ct)
                    : DefaultEndpoint();

                var url = $"{ep.BaseUrl}/api/Receipt/{Uri.EscapeDataString(trackId)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyAuth(request, ep);

                using var response = await SendAsync(request, ep.TimeoutSeconds, ct);
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
                var consulta = PgEInvoicingMapper.ToConsultaResultado(wireResp, trackId);

                if (!string.IsNullOrWhiteSpace(consulta.SecurityCode) || !string.IsNullOrWhiteSpace(consulta.UrlQR))
                {
                    var ecf = await _ctx.ECFEncabezados.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.TrackId == trackId, ct);
                    if (ecf != null)
                    {
                        var empresa = await _ctx.Empresas.AsNoTracking()
                            .FirstOrDefaultAsync(e => e.IdEmpresa == ecf.IdEmpresa, ct);
                        consulta.UrlQR = EcfQrUrlHelper.ResolveFromEncabezadoFields(
                            consulta.UrlQR,
                            empresa?.AmbienteFE,
                            empresa?.RNC ?? "",
                            ecf.RncReceptor,
                            ecf.ENCF ?? consulta.Encf ?? "",
                            ecf.FechaEmision,
                            ecf.TotalGeneral,
                            consulta.FechaFirma ?? ecf.FechaFirma,
                            consulta.SecurityCode ?? ecf.SecurityCode);
                    }
                    else if (!EcfQrUrlHelper.IsUsableHttpUrl(consulta.UrlQR))
                    {
                        consulta.UrlQR = null;
                    }
                }

                return consulta;
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
                var ep = DefaultEndpoint();
                // Alahia.eCF.Api acepta GET /api/Receipt; Invoice no — usar pendienteAprobacion si BaseUrl parece externo.
                var path = ep.BaseUrl.Contains("einvoicing", StringComparison.OrdinalIgnoreCase)
                    ? $"{ep.BaseUrl}/api/Receipt/pendienteAprobacion"
                    : $"{ep.BaseUrl}/api/Receipt";
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                ApplyAuth(request, ep);
                using var response = await SendAsync(request, Math.Min(15, ep.TimeoutSeconds), ct);
                var code = (int)response.StatusCode;
                return response.IsSuccessStatusCode || code is 401 or 403 or 404;
            }
            catch
            {
                return false;
            }
        }

        private async Task<FiscalGatewayEndpoint> ResolveEndpointAsync(int idEmpresa, CancellationToken ct)
        {
            if (idEmpresa > 0)
                return await _resolver.ResolveAsync(idEmpresa, ct);
            return DefaultEndpoint();
        }

        private FiscalGatewayEndpoint DefaultEndpoint() => new()
        {
            Modo = ProveedorFiscalHelper.DgiiDirecto,
            Nombre = "Alahia.eCF.Api",
            BaseUrl = _settings.BaseUrl.Trim().TrimEnd('/'),
            ApiKey = _settings.ApiKey,
            TimeoutSeconds = _settings.TimeoutSeconds > 0 ? _settings.TimeoutSeconds : 30
        };

        private async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, int timeoutSeconds, CancellationToken ct)
        {
            var client = _httpFactory.CreateClient(nameof(HttpReceiptFiscalGateway));
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 30);
            return await client.SendAsync(request, ct);
        }

        private static void ApplyAuth(HttpRequestMessage request, FiscalGatewayEndpoint ep)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(ep.ApiKey))
                request.Headers.TryAddWithoutValidation("X-Api-Key", ep.ApiKey);
            if (!string.IsNullOrWhiteSpace(ep.Usuario) && !string.IsNullOrWhiteSpace(ep.Password))
            {
                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ep.Usuario}:{ep.Password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", token);
            }
        }

        private static string Truncar(string? s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";
    }
}
