using AlahiaPos.Entities.Interfaces;
using Google.Apis.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios
{
    public class NotificationServices : INotification
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private readonly string _apiKey;

        public NotificationServices(IConfiguration config)
        {
            _http = new HttpClient();
            _appId = config["OneSignal:AppId"];
            _apiKey = config["OneSignal:ApiKey"];
        }



        // ============================================================
        // 🔥 MÉTODO GENERAL PARA ENVIAR NOTIFICACIONES A ONESIGNAL
        // ============================================================
        private async Task<bool> SendAsync(object payload)
        {
            var json = JsonConvert.SerializeObject(payload);

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://onesignal.com/api/v1/notifications"
            );

            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // 🔥 AUTH CORRECTA PARA ONESIGNAL V16
            req.Headers.Add("Authorization", $"Bearer {_apiKey}");

            var resp = await _http.SendAsync(req);

            string result = await resp.Content.ReadAsStringAsync();
            Console.WriteLine("📨 OneSignal Response: " + result);

            return resp.IsSuccessStatusCode;
        }


        // ============================================================
        // 1️⃣ ENVIAR POR TAG ESPECÍFICO (Método oficial de la interfaz)
        // ============================================================
        public async Task<bool> EnviarNotificacionPorTagAsync(
     string tagKey, string tagValue, string titulo, string cuerpo)
        {
            var payload = new
            {
                app_id = _appId,
                target_channel = "web_push",

                // 🔥 Este es el verdadero ID único para evitar reemplazar notificaciones
                web_push_topic = Guid.NewGuid().ToString(),

                filters = new[] {
            new {
                field = "tag",
                key = tagKey,
                relation = "=",
                value = tagValue
            }
        },

                headings = new { en = titulo },
                contents = new { en = cuerpo }
            };

            return await SendAsync(payload);
        }





        // ============================================================
        // 2️⃣ ENVIAR POR EMPRESA (usa tag empresa_id)
        // ============================================================
        public async Task<bool> EnviarPorEmpresaAsync(int idEmpresa, string titulo, string cuerpo)
        {
            var payload = new
            {
                app_id = _appId,

                filters = new[]
                {
                    new {
                        field = "tag",
                        key = "empresa_id",
                        relation = "=",
                        value = idEmpresa.ToString()
                    }
                },

                headings = new { es = titulo },
                contents = new { es = cuerpo }
            };

            return await SendAsync(payload);
        }

        // ============================================================
        // 3️⃣ ENVIAR A TODOS LOS DISPOSITIVOS
        // ============================================================
        public async Task<bool> EnviarATodosAsync(string titulo, string cuerpo)
        {
            var payload = new
            {
                app_id = _appId,
                included_segments = new[] { "All" },

                headings = new { es = titulo },
                contents = new { es = cuerpo }
            };

            return await SendAsync(payload);
        }
    }
}
