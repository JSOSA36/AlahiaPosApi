using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Setting;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DgiiClientService : IDgiiClientService
    {
        private readonly HttpClient _http;
        private readonly DgiiSettings _cfg;
        private readonly IECFSigner _signer;

        // Cache simple del token (para no pedir semilla en cada envío)
        private static readonly SemaphoreSlim _tokenLock = new(1, 1);
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiraUtc = DateTimeOffset.MinValue;

        public DgiiClientService(HttpClient http, IOptions<DgiiSettings> cfg, IECFSigner signer)
        {
            _http = http;
            _cfg = cfg.Value;
            _signer = signer;

            _http.Timeout = TimeSpan.FromSeconds(60);

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }
        public async Task<object> EnviarRFCE_Debug(string xmlFirmado)
        {
            var token = await Autenticar();

            var url = CombineUrl(_cfg.HostFc, _cfg.BaseUrlFc, _cfg.RecepcionRfceEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);

            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var form = new MultipartFormDataContent();

            var xmlBytes = Encoding.UTF8.GetBytes(xmlFirmado);

            var fileContent = new ByteArrayContent(xmlBytes);
            fileContent.Headers.ContentType =
                new MediaTypeHeaderValue("application/xml");

            form.Add(fileContent, "xml", "rfce.xml");

            req.Content = form;

            using var res = await _http.SendAsync(req);

            var body = await res.Content.ReadAsStringAsync();

            return new
            {
                url,
                token,              // 👈 útil para debug
                xmlEnviado = xmlFirmado, // 👈 ESTE ES EL IMPORTANTE
                status = (int)res.StatusCode,
                ok = res.IsSuccessStatusCode,
                responseBody = body
            };
        }
        // =====================================================
        // 🔐 OBTENER SEMILLA (OpenAPI: GET /api/Autenticacion/Semilla)
        // Base: https://ecf.dgii.gov.do/CerteCF/Autenticacion
        // =====================================================
        public async Task<string> ObtenerSemilla()
        {
            var url = CombineUrl(_cfg.HostEcf, _cfg.BaseUrlAuth, _cfg.SemillaEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Error obteniendo semilla DGII\n" +
                    $"URL: {url}\n" +
                    $"Status: {(int)res.StatusCode}\n" +
                    $"Respuesta: {body}"
                );
            }

            return body; // XML (SemillaModel)
        }

        // =====================================================
        // 🔐 VALIDAR SEMILLA (OpenAPI: POST /api/Autenticacion/ValidarSemilla)
        // multipart/form-data con field "xml"
        // =====================================================
        public async Task<string> ValidarSemilla(string semillaFirmadaXml)
        {
            var url = CombineUrl(_cfg.HostEcf, _cfg.BaseUrlAuth, _cfg.ValidarSemillaEndpoint);

            using var form = new MultipartFormDataContent();

            var bytes = Encoding.UTF8.GetBytes(semillaFirmadaXml);
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            // field requerido por OpenAPI: "xml"
            form.Add(fileContent, "xml", "semilla_firmada.xml");

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            req.Content = form;

            var (ok, body, status) = await Send(req);

            if (!ok)
            {
                throw new Exception(
                    $"Error validando semilla DGII\n" +
                    $"URL: {url}\n" +
                    $"Status: {status}\n" +
                    $"Respuesta: {body}"
                );
            }

            return body; // JSON o XML (según DGII)
        }

        // =====================================================
        // 🔐 AUTENTICAR (Semilla -> Firmar XML -> Validar -> Token)
        // =====================================================
        public async Task<string> Autenticar()
        {
            if (!string.IsNullOrWhiteSpace(_cfg.TokenFijo))
                return _cfg.TokenFijo!;

            if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiraUtc)
                return _cachedToken!;

            await _tokenLock.WaitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiraUtc)
                    return _cachedToken!;

                // 1) Obtener semilla (XML)
                var semillaXml = await ObtenerSemilla();

                // 2) Firmar el XML completo de la semilla (XMLDSIG) con tu firmador
                var semillaFirmadaXml = _signer.Firmar(
                    semillaXml,
                    _cfg.P12Path,
                    _cfg.P12Password
                );

                // 3) Validar semilla (multipart)
                var resp = await ValidarSemilla(semillaFirmadaXml);

                // 4) Parsear token/expira
                var (token, expiraUtc) = ParseTokenResponse(resp);

                _cachedToken = token;
                _tokenExpiraUtc = expiraUtc;

                return token;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // =====================================================
        // 📤 ENVIAR ECF (OpenAPI: POST /CerteCF/Recepcion/api/FacturasElectronicas)
        // multipart/form-data con field "xml"
        // =====================================================
        public async Task<DtoRespuestaDgii> EnviarECF(string xmlFirmado)
        {
            var token = await Autenticar();

            var url = CombineUrl(_cfg.HostEcf, _cfg.BaseUrlEcf, _cfg.RecepcionEcfEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var form = new MultipartFormDataContent();

            var xmlBytes = Encoding.UTF8.GetBytes(xmlFirmado);
            var fileContent = new ByteArrayContent(xmlBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            form.Add(fileContent, "xml", "ecf.xml");
            req.Content = form;

            var (ok, body, status) = await Send(req);
            return ParseRespuesta(ok, body, status);
        }

        // =====================================================
        // 📤 ENVIAR RFCE (OpenAPI: POST /certecf/recepcionfc/api/recepcion/ecf)
        // multipart/form-data con field "xml"
        // =====================================================
        public async Task<DtoRespuestaDgii> EnviarRFCE(string xmlFirmado)
        {
            var token = await Autenticar();

            var url = CombineUrl(_cfg.HostFc, _cfg.BaseUrlFc, _cfg.RecepcionRfceEndpoint);

            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var form = new MultipartFormDataContent();

            var xmlBytes = Encoding.UTF8.GetBytes(xmlFirmado);
            var fileContent = new ByteArrayContent(xmlBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            form.Add(fileContent, "xml", "rfce.xml");
            req.Content = form;

            var (ok, body, status) = await Send(req);
            return ParseRespuesta(ok, body, status);
        }

        // =====================================================
        // 🔎 CONSULTAR RESULTADO (OpenAPI: GET /CerteCF/ConsultaResultado/api/Consultas/Estado?TrackId=...)
        // =====================================================
        public async Task<DtoRespuestaDgii> ConsultarResultado(string trackId)
        {
            var token = await Autenticar();

            var baseUrl = CombineUrl(_cfg.HostEcf, _cfg.BaseUrlConsulta, _cfg.ConsultaEstadoEndpoint);
            var url = $"{baseUrl}?TrackId={Uri.EscapeDataString(trackId)}";


            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var (ok, body, status) = await Send(req);

            var resp = ParseRespuesta(ok, body, status);
            resp.TrackId = trackId;

            return resp;
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string CombineUrl(string host, string basePath, string endpoint)
        {
            host = host.TrimEnd('/');
            basePath = basePath.Trim('/');
            endpoint = endpoint.TrimStart('/');

            return $"{host}/{basePath}/{endpoint}";
        }

        private async Task<(bool ok, string body, int status)> Send(HttpRequestMessage req)
        {
            using var res = await _http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            return (res.IsSuccessStatusCode, body, (int)res.StatusCode);
        }

        private static (string token, DateTimeOffset expiraUtc) ParseTokenResponse(string resp)
        {
            // OpenAPI autenticación:
            // JSON: { token: "...", expira: "date-time", expedido: "date-time" }
            // XML:  <RespuestaAutenticacion><token>...</token><expira>...</expira>...</RespuestaAutenticacion>

            // 1) Intentar JSON
            try
            {
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;

                var token =
                    TryGet(root, "token") ??
                    TryGet(root, "Token");

                if (string.IsNullOrWhiteSpace(token))
                    throw new Exception("DGII no devolvió token en la respuesta.");

                var expiraStr =
                    TryGet(root, "expira") ??
                    TryGet(root, "Expira");

                if (!string.IsNullOrWhiteSpace(expiraStr) &&
                    DateTimeOffset.TryParse(expiraStr, out var expira))
                {
                    return (token!, expira.ToUniversalTime().AddSeconds(-60));
                }

                return (token!, DateTimeOffset.UtcNow.AddMinutes(55));
            }
            catch (JsonException)
            {
                // 2) Intentar XML
                try
                {
                    var xdoc = XDocument.Parse(resp);

                    var token = xdoc.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName.Equals("token", StringComparison.OrdinalIgnoreCase))
                        ?.Value?.Trim();

                    if (string.IsNullOrWhiteSpace(token))
                        throw new Exception("DGII no devolvió <token> en la respuesta XML.");

                    var expiraStr = xdoc.Descendants()
                        .FirstOrDefault(x => x.Name.LocalName.Equals("expira", StringComparison.OrdinalIgnoreCase))
                        ?.Value?.Trim();

                    if (!string.IsNullOrWhiteSpace(expiraStr) &&
                        DateTimeOffset.TryParse(expiraStr, out var expira))
                    {
                        return (token!, expira.ToUniversalTime().AddSeconds(-60));
                    }

                    return (token!, DateTimeOffset.UtcNow.AddMinutes(55));
                }
                catch
                {
                    throw new Exception("Respuesta de ValidarSemilla no es JSON/XML válido. Respuesta: " + resp);
                }
            }
        }

        private static DtoRespuestaDgii ParseRespuesta(bool ok, string body, int status)
        {
            // Soporta:
            // - Recepción eCF: { trackId, error, mensaje }
            // - RFCE: { codigo, estado, mensajes:[{codigo,valor}], encf, secuenciaUtilizada }
            // - Consulta: { trackId, codigo, estado, rnc, encf, secuenciaUtilizada, fechaRecepcion, mensajes[...] }

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                string? trackId =
                    TryGet(root, "trackId") ??
                    TryGet(root, "TrackId");

                string? codigo =
                    TryGet(root, "codigo") ??
                    TryGet(root, "Codigo");

                string? mensaje =
                    TryGet(root, "mensaje") ??
                    TryGet(root, "Mensaje") ??
                    TryGet(root, "error") ??
                    TryGet(root, "Error");

                if (string.IsNullOrWhiteSpace(mensaje) &&
                    root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("mensajes", out var msgs))
                {
                    mensaje = msgs.ToString();
                }

                return new DtoRespuestaDgii
                {
                    CodigoError = codigo ?? (ok ? "0" : status.ToString()),
                    Mensaje = mensaje ?? body,
                    TrackId = trackId
                };
            }
            catch
            {
                // Si DGII devolviera XML o texto plano, no inventamos parsing.
                return new DtoRespuestaDgii
                {
                    CodigoError = ok ? "0" : status.ToString(),
                    Mensaje = body,
                    TrackId = null
                };
            }
        }

        private static string? TryGet(JsonElement el, string prop)
        {
            if (el.ValueKind == JsonValueKind.Object &&
                el.TryGetProperty(prop, out var p))
            {
                return p.ValueKind == JsonValueKind.String
                    ? p.GetString()
                    : p.ToString();
            }

            return null;
        }
    }
}