using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Delivery;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Delivery
{
    public class OsrmDeliveryDistanceProvider : IDeliveryDistanceProvider
    {
        public const string HttpClientName = "osrm";

        private readonly IHttpClientFactory _http;
        private readonly ILogger<OsrmDeliveryDistanceProvider> _log;

        public OsrmDeliveryDistanceProvider(IHttpClientFactory http, ILogger<OsrmDeliveryDistanceProvider> log)
        {
            _http = http;
            _log = log;
        }

        public string Nombre => "OSRM";

        public async Task<DeliveryMedicion> MedirAsync(
            decimal origenLat,
            decimal origenLng,
            decimal destinoLat,
            decimal destinoLng,
            CancellationToken cancellationToken)
        {
            var cultura = CultureInfo.InvariantCulture;
            var path = string.Format(
                cultura,
                "route/v1/driving/{0},{1};{2},{3}?overview=false",
                origenLng, origenLat, destinoLng, destinoLat);

            try
            {
                var client = _http.CreateClient(HttpClientName);
                using var response = await client.GetAsync(path, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("OSRM respondió {Status} al medir delivery.", (int)response.StatusCode);
                    return Fallo("El proveedor de rutas no respondió.");
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var code = root.TryGetProperty("code", out var codeEl) ? codeEl.GetString() : "";
                if (!string.Equals(code, "Ok", StringComparison.OrdinalIgnoreCase)
                    || !root.TryGetProperty("routes", out var routes)
                    || routes.GetArrayLength() == 0)
                {
                    _log.LogWarning("OSRM no encontró ruta. Código {Code}.", code);
                    return Fallo("No hay una ruta válida entre la sucursal y el cliente.");
                }

                var route = routes[0];
                if (!route.TryGetProperty("distance", out var distanceEl))
                    return Fallo("La ruta no trajo distancia.");

                var metros = distanceEl.GetDecimal();
                if (metros < 0)
                    return Fallo("La ruta devolvió una distancia inválida.");

                int? segundos = null;
                if (route.TryGetProperty("duration", out var durationEl) && durationEl.TryGetDecimal(out var segs))
                    segundos = (int)decimal.Round(segs, 0, MidpointRounding.AwayFromZero);

                return new DeliveryMedicion
                {
                    Ok = true,
                    DistanciaKm = decimal.Round(metros / 1000m, 3, MidpointRounding.AwayFromZero),
                    DuracionSegundos = segundos,
                    Proveedor = Nombre
                };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                _log.LogWarning(ex, "OSRM falló al medir delivery.");
                return Fallo("El proveedor de rutas no respondió.");
            }
        }

        private DeliveryMedicion Fallo(string error)
            => new() { Ok = false, Proveedor = Nombre, Error = error };
    }
}
