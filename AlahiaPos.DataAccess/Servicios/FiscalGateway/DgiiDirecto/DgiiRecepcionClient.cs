using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    public class DgiiHttpResultado
    {
        public bool Ok { get; set; }
        public int StatusCode { get; set; }
        public string Body { get; set; } = "";
        public string? TrackId { get; set; }
        public string? Codigo { get; set; }
        public string? Estado { get; set; }
        public string? Encf { get; set; }
        public string? Rnc { get; set; }
        public string? Mensaje { get; set; }
        public List<string> Mensajes { get; set; } = new();
        public bool? SecuenciaUtilizada { get; set; }
        public DateTime? FechaRecepcion { get; set; }
    }

    public class DgiiRecepcionClient
    {
        private readonly HttpClient _http;
        private readonly DgiiDirectoSettings _settings;
        private readonly DgiiAuthService _auth;
        private readonly ILogger<DgiiRecepcionClient> _logger;

        public DgiiRecepcionClient(
            HttpClient http,
            IOptions<DgiiDirectoSettings> settings,
            DgiiAuthService auth,
            ILogger<DgiiRecepcionClient> logger)
        {
            _http = http;
            _settings = settings.Value;
            _auth = auth;
            _logger = logger;
            _http.Timeout = TimeSpan.FromSeconds(Math.Max(15, _settings.TimeoutSeconds));
        }

        private DgiiDirectoSettings Eff => _settings.Effective();

        public async Task<DgiiHttpResultado> EnviarRfceAsync(
            string xmlFirmado,
            string nombreArchivo,
            int idEmpresa,
            CancellationToken ct = default)
        {
            var eff = Eff;
            var token = await _auth.ObtenerTokenAsync(idEmpresa, ct);
            var url = Combine(eff.RecepcionFcBaseUrl, eff.RecepcionRfceEndpoint);

            using var form = new MultipartFormDataContent();
            var bytes = Encoding.UTF8.GetBytes(xmlFirmado);
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            form.Add(file, "xml", string.IsNullOrWhiteSpace(nombreArchivo) ? "rfce.xml" : nombreArchivo);

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("DGII RFCE POST {Url} ambiente={Ambiente} archivo={Archivo}", url, eff.AmbientePath, nombreArchivo);
            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return Parse(res.IsSuccessStatusCode, (int)res.StatusCode, body);
        }

        public async Task<DgiiHttpResultado> EnviarEcfAsync(
            string xmlFirmado,
            string nombreArchivo,
            int idEmpresa,
            CancellationToken ct = default)
        {
            var eff = Eff;
            var token = await _auth.ObtenerTokenAsync(idEmpresa, ct);
            var url = Combine(eff.RecepcionBaseUrl, eff.RecepcionEcfEndpoint);

            using var form = new MultipartFormDataContent();
            var bytes = Encoding.UTF8.GetBytes(xmlFirmado);
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            form.Add(file, "xml", string.IsNullOrWhiteSpace(nombreArchivo) ? "ecf.xml" : nombreArchivo);

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("DGII recepción POST {Url} ambiente={Ambiente} archivo={Archivo}", url, eff.AmbientePath, nombreArchivo);
            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return Parse(res.IsSuccessStatusCode, (int)res.StatusCode, body);
        }

        public async Task<DgiiHttpResultado> EnviarAcecfAsync(
            string xmlFirmado,
            string nombreArchivo,
            int idEmpresa,
            CancellationToken ct = default)
        {
            var eff = Eff;
            var token = await _auth.ObtenerTokenAsync(idEmpresa, ct);
            var url = Combine(eff.AprobacionComercialBaseUrl, eff.AprobacionComercialEndpoint);

            using var form = new MultipartFormDataContent();
            var bytes = Encoding.UTF8.GetBytes(xmlFirmado);
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
            form.Add(file, "xml", string.IsNullOrWhiteSpace(nombreArchivo) ? "acecf.xml" : nombreArchivo);

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation("DGII ACECF POST {Url} ambiente={Ambiente} archivo={Archivo}", url, eff.AmbientePath, nombreArchivo);
            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            return Parse(res.IsSuccessStatusCode, (int)res.StatusCode, body);
        }

        public async Task<DgiiHttpResultado> ConsultarEstadoAsync(
            string trackId,
            int idEmpresa,
            CancellationToken ct = default)
        {
            var eff = Eff;
            var token = await _auth.ObtenerTokenAsync(idEmpresa, ct);
            var baseUrl = Combine(eff.ConsultaBaseUrl, eff.ConsultaEstadoEndpoint);
            var url = $"{baseUrl}?TrackId={Uri.EscapeDataString(trackId)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            var parsed = Parse(res.IsSuccessStatusCode, (int)res.StatusCode, body);
            parsed.TrackId ??= trackId;
            return parsed;
        }

        private static string Combine(string baseUrl, string endpoint)
        {
            baseUrl = baseUrl.TrimEnd('/');
            endpoint = endpoint.StartsWith('/') ? endpoint : "/" + endpoint;
            return baseUrl + endpoint;
        }

        private static DgiiHttpResultado Parse(bool ok, int status, string body)
        {
            var result = new DgiiHttpResultado
            {
                Ok = ok,
                StatusCode = status,
                Body = body
            };

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                result.TrackId = TryGet(root, "trackId") ?? TryGet(root, "TrackId");
                result.Codigo = TryGet(root, "codigo") ?? TryGet(root, "Codigo");
                result.Estado = TryGet(root, "estado") ?? TryGet(root, "Estado");
                result.Encf = TryGet(root, "encf") ?? TryGet(root, "eNCF") ?? TryGet(root, "ENCF");
                result.Rnc = TryGet(root, "rnc") ?? TryGet(root, "Rnc");
                result.Mensaje = TryGet(root, "mensaje") ?? TryGet(root, "Mensaje")
                    ?? TryGet(root, "error") ?? TryGet(root, "Error");

                if (root.TryGetProperty("mensajes", out var msgs) || root.TryGetProperty("Mensajes", out msgs))
                {
                    if (msgs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var m in msgs.EnumerateArray())
                        {
                            var v = TryGet(m, "valor") ?? TryGet(m, "Valor") ?? m.ToString();
                            if (!string.IsNullOrWhiteSpace(v)) result.Mensajes.Add(v!);
                        }
                    }
                }

                if (root.TryGetProperty("secuenciaUtilizada", out var su) || root.TryGetProperty("SecuenciaUtilizada", out su))
                {
                    if (su.ValueKind == JsonValueKind.True || su.ValueKind == JsonValueKind.False)
                        result.SecuenciaUtilizada = su.GetBoolean();
                }

                var fecha = TryGet(root, "fechaRecepcion") ?? TryGet(root, "FechaRecepcion");
                if (!string.IsNullOrWhiteSpace(fecha) && DateTime.TryParse(fecha, out var fr))
                    result.FechaRecepcion = fr;
            }
            catch
            {
                result.Mensaje ??= body;
            }

            if (result.Mensajes.Count == 0 && !string.IsNullOrWhiteSpace(result.Mensaje))
                result.Mensajes.Add(result.Mensaje);

            return result;
        }

        private static string? TryGet(JsonElement el, string prop)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var p))
                return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
            return null;
        }
    }
}
