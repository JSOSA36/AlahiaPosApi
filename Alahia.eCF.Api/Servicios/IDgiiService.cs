using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Alahia.eCF.Api.Interfaces;
using Alahia.eCF.Api.Setting;

namespace Alahia.eCF.Api.Services
{
    public class DgiiService : IDgiiService
    {
        private readonly HttpClient _http;
        private readonly DgiiSettings _settings;

        private readonly string _urlSemilla;
        private readonly string _urlToken;
        private readonly string _urlRecepcion;
        private readonly string _urlConsulta;

        public DgiiService(HttpClient http, IOptions<DgiiSettings> settings)
        {
            _http = http;
            _settings = settings.Value;

            var baseAuth = _settings.HostEcf + _settings.BaseUrlAuth;
            var baseEcf = _settings.HostEcf + _settings.BaseUrlEcf;

            _urlSemilla = baseAuth + _settings.SemillaEndpoint;
            _urlToken = baseAuth + _settings.ValidarSemillaEndpoint;
            _urlRecepcion = baseEcf + _settings.RecepcionEcfEndpoint;
            _urlConsulta = baseEcf + _settings.ConsultaEstadoEndpoint;
        }

        // 🔹 1. Obtener semilla
        public async Task<string> ObtenerSemilla()
        {
            var response = await _http.GetAsync(_urlSemilla);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // 🔹 2. Obtener token (validar semilla)
        public async Task<string> ObtenerToken(string semillaFirmadaXml)
        {
            using var form = new MultipartFormDataContent();

            var bytes = Encoding.UTF8.GetBytes(semillaFirmadaXml);

            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            // 🔥 ESTE NOMBRE ES CRÍTICO
            form.Add(fileContent, "xml", "semilla_firmada.xml");

            var request = new HttpRequestMessage(HttpMethod.Post, _urlToken);
            request.Content = form;

            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error DGII ValidarSemilla: {result}");
            }

            return result;
        }

        // 🔥 3. Enviar e-CF
        public async Task<string> EnviarEcf(byte[] xmlFirmado, string fileName, string token)
        {
            using var content = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(xmlFirmado);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            content.Add(fileContent, "archivo", fileName);

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.PostAsync(_urlRecepcion, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        // 🔹 4. Consultar estado
        public async Task<string> ConsultarEstado(string trackId, string token)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"{_urlConsulta}?trackId={trackId}";

            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}