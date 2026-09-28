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
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
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
                var ambiente = (documento.AmbienteDgii ?? "").Trim();
                var usarFiscalNativo = ep.Modo == ProveedorFiscalHelper.DgiiDirecto;
                string json;
                string url;
                if (usarFiscalNativo)
                {
                    json = JsonSerializer.Serialize(documento, JsonWrite);
                    url = $"{ep.BaseUrl}/api/Receipt/fiscal";
                    GuardarWireCertecf(documento.Encabezado.Encf, json);
                }
                else
                {
                    var wire = PgEInvoicingMapper.ToProviderModel(documento);
                    json = JsonSerializer.Serialize(wire, JsonWrite);
                    url = $"{ep.BaseUrl}/api/Receipt";
                }

                _logger.LogInformation(
                    "Gateway: enviando e-CF {Encf} tipo {Tipo} modo={Modo} ambiente={Ambiente} → {Url}",
                    documento.Encabezado.Encf, documento.Encabezado.TipoEcf, ep.Modo, ambiente, url);

                var response = await PostJsonAsync(url, json, ep, usarFiscalNativo ? ambiente : null, usarFiscalNativo ? documento.IdEmpresa : 0, ct);
                string responseBody;
                int status;
                bool ok;
                try
                {
                    responseBody = await response.Content.ReadAsStringAsync(ct);
                    status = (int)response.StatusCode;
                    ok = response.IsSuccessStatusCode;
                    // API e-CF anterior (julio) no tiene POST /fiscal: "fiscal" cae en GET {trackId} → 405.
                    if (usarFiscalNativo && status == 405)
                    {
                        _logger.LogWarning(
                            "Gateway: {Url} devolvió 405; reintento POST /api/Receipt (contrato PG) para {Encf}",
                            url, documento.Encabezado.Encf);
                        response.Dispose();
                        var wire = PgEInvoicingMapper.ToProviderModel(documento);
                        json = JsonSerializer.Serialize(wire, JsonWrite);
                        url = $"{ep.BaseUrl}/api/Receipt";
                        response = await PostJsonAsync(url, json, ep, ambiente, documento.IdEmpresa, ct);
                        responseBody = await response.Content.ReadAsStringAsync(ct);
                        status = (int)response.StatusCode;
                        ok = response.IsSuccessStatusCode;
                    }
                }
                finally
                {
                    response.Dispose();
                }

                if (!ok)
                {
                    _logger.LogWarning(
                        "Gateway: HTTP {Status} para {Encf}: {Body}",
                        status, documento.Encabezado.Encf, Truncar(responseBody, 500));

                    return FiscalEnvioResultado.Error(
                        $"HTTP_{status}",
                        $"Proveedor retornó {(System.Net.HttpStatusCode)status}: {Truncar(responseBody, 500)}");
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
            int idEmpresa = 0,
            CancellationToken ct = default)
        {
            try
            {
                var id = idEmpresa;
                if (id <= 0)
                {
                    id = await _ctx.ECFEncabezados.AsNoTracking()
                        .Where(e => e.TrackId == trackId)
                        .Select(e => e.IdEmpresa)
                        .FirstOrDefaultAsync(ct);
                }

                var ep = id > 0
                    ? await _resolver.ResolveAsync(id, ct)
                    : DefaultEndpoint();

                var url = $"{ep.BaseUrl}/api/Receipt/{Uri.EscapeDataString(trackId)}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                ApplyAuth(request, ep);
                if (ep.Modo == ProveedorFiscalHelper.DgiiDirecto && id > 0)
                {
                    request.Headers.TryAddWithoutValidation("X-Dgii-IdEmpresa", id.ToString());
                    var ambiente = await _ctx.Empresas.AsNoTracking()
                        .Where(e => e.IdEmpresa == id)
                        .Select(e => e.AmbienteFE)
                        .FirstOrDefaultAsync(ct);
                    if (!string.IsNullOrWhiteSpace(ambiente))
                        request.Headers.TryAddWithoutValidation("X-Dgii-Ambiente", ambiente);
                }

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

        private async Task<HttpResponseMessage> PostJsonAsync(
            string url,
            string json,
            FiscalGatewayEndpoint ep,
            string? ambiente,
            int idEmpresa,
            CancellationToken ct)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            ApplyAuth(request, ep);
            if (!string.IsNullOrWhiteSpace(ambiente))
                request.Headers.TryAddWithoutValidation("X-Dgii-Ambiente", ambiente);
            if (idEmpresa > 0)
                request.Headers.TryAddWithoutValidation("X-Dgii-IdEmpresa", idEmpresa.ToString());
            return await SendAsync(request, ep.TimeoutSeconds, ct);
        }

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

        private static void GuardarWireCertecf(string? encf, string json)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Documents", "GitHub", "AlahiaPosApi", "artifacts", "gold-testecf");
                Directory.CreateDirectory(dir);
                var name = string.IsNullOrWhiteSpace(encf) ? "sin-encf" : encf.Trim();
                File.WriteAllText(Path.Combine(dir, $"wire_{name}.json"), json);
            }
            catch
            {
                // no bloquear envío
            }
        }

        private static string Truncar(string? s, int max)
            => string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";
    }
}
