using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    public class DgiiAuthService
    {
        private readonly HttpClient _http;
        private readonly DgiiDirectoSettings _settings;
        private readonly DgiiCertificadoResolver _certs;
        private readonly ILogger<DgiiAuthService> _logger;

        private static readonly SemaphoreSlim TokenLock = new(1, 1);
        private static readonly ConcurrentDictionary<string, (string Token, DateTimeOffset ExpiraUtc)> TokenCache = new();

        public DgiiAuthService(
            HttpClient http,
            IOptions<DgiiDirectoSettings> settings,
            DgiiCertificadoResolver certs,
            ILogger<DgiiAuthService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _certs = certs;
            _logger = logger;
            _http.Timeout = TimeSpan.FromSeconds(Math.Max(15, _settings.TimeoutSeconds));
        }

        private DgiiDirectoSettings Eff => _settings.Effective();

        public async Task<string> ObtenerTokenAsync(int idEmpresa, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(_settings.TokenFijo))
                return _settings.TokenFijo!;

            if (idEmpresa <= 0)
                idEmpresa = DgiiEmpresaContext.Current;

            var ambiente = Eff.AmbientePath;
            var cacheKey = $"{ambiente}|{idEmpresa}";

            if (TokenCache.TryGetValue(cacheKey, out var cached) && DateTimeOffset.UtcNow < cached.ExpiraUtc)
                return cached.Token;

            await TokenLock.WaitAsync(ct);
            try
            {
                if (TokenCache.TryGetValue(cacheKey, out cached) && DateTimeOffset.UtcNow < cached.ExpiraUtc)
                    return cached.Token;

                var semillaXml = await ObtenerSemillaAsync(ct);
                var material = await _certs.ResolveAsync(idEmpresa, ct);
                var semillaFirmada = _certs.Firmar(semillaXml, material);
                var resp = await ValidarSemillaAsync(semillaFirmada, ct);
                var (token, expira) = ParseTokenResponse(resp);

                // Key también por fuente de cert para no mezclar
                var key = $"{ambiente}|{idEmpresa}|{material.Source}";
                TokenCache[cacheKey] = (token, expira);
                TokenCache[key] = (token, expira);

                _logger.LogInformation(
                    "DGII auth OK ambiente={Ambiente} url={Url} cert={Source} expira={Expira}",
                    ambiente, Eff.AuthBaseUrl, material.Source, expira);
                return token;
            }
            finally
            {
                TokenLock.Release();
            }
        }

        public async Task<string> ObtenerSemillaAsync(CancellationToken ct = default)
        {
            var eff = Eff;
            var url = Combine(eff.AuthBaseUrl, eff.SemillaEndpoint);
            _logger.LogInformation("DGII semilla GET {Url}", url);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error semilla DGII ({(int)res.StatusCode}) [{eff.AmbientePath}]: {body}");

            return body;
        }

        public async Task<string> ValidarSemillaAsync(string semillaFirmadaXml, CancellationToken ct = default)
        {
            var eff = Eff;
            var url = Combine(eff.AuthBaseUrl, eff.ValidarSemillaEndpoint);
            using var form = new MultipartFormDataContent();
            var bytes = Encoding.UTF8.GetBytes(semillaFirmadaXml);
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
            form.Add(file, "xml", "semilla_firmada.xml");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            using var res = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Error ValidarSemilla ({(int)res.StatusCode}) [{eff.AmbientePath}]: {body}");

            return body;
        }

        public async Task<bool> VerificarConexionAsync(CancellationToken ct = default)
        {
            try
            {
                _ = await ObtenerSemillaAsync(ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DGII health check falló ambiente={Ambiente}", Eff.AmbientePath);
                return false;
            }
        }

        private static string Combine(string baseUrl, string endpoint)
        {
            baseUrl = baseUrl.TrimEnd('/');
            endpoint = endpoint.StartsWith('/') ? endpoint : "/" + endpoint;
            return baseUrl + endpoint;
        }

        private static (string token, DateTimeOffset expiraUtc) ParseTokenResponse(string resp)
        {
            try
            {
                using var doc = JsonDocument.Parse(resp);
                var root = doc.RootElement;
                var token = TryGet(root, "token") ?? TryGet(root, "Token");
                if (string.IsNullOrWhiteSpace(token))
                    throw new InvalidOperationException("DGII no devolvió token.");

                var expiraStr = TryGet(root, "expira") ?? TryGet(root, "Expira");
                if (!string.IsNullOrWhiteSpace(expiraStr) && DateTimeOffset.TryParse(expiraStr, out var expira))
                    return (token!, expira.ToUniversalTime().AddSeconds(-60));

                return (token!, DateTimeOffset.UtcNow.AddMinutes(55));
            }
            catch (JsonException)
            {
                var xdoc = XDocument.Parse(resp);
                var token = xdoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName.Equals("token", StringComparison.OrdinalIgnoreCase))
                    ?.Value?.Trim();
                if (string.IsNullOrWhiteSpace(token))
                    throw new InvalidOperationException("DGII no devolvió <token>. Respuesta: " + resp);

                var expiraStr = xdoc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName.Equals("expira", StringComparison.OrdinalIgnoreCase))
                    ?.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(expiraStr) && DateTimeOffset.TryParse(expiraStr, out var expira))
                    return (token!, expira.ToUniversalTime().AddSeconds(-60));

                return (token!, DateTimeOffset.UtcNow.AddMinutes(55));
            }
        }

        private static string? TryGet(JsonElement el, string prop)
        {
            if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty(prop, out var p))
                return p.ValueKind == JsonValueKind.String ? p.GetString() : p.ToString();
            return null;
        }
    }
}
