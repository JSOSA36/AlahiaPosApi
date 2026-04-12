using AlahiaPos.DGII.Sync.Interfaces;

namespace AlahiaPos.DGII.Sync.Servicios
{
    public class DgiiDownloader : IDgiiDownloader
    {
        private readonly HttpClient _http;
        private readonly ILogger<DgiiDownloader> _logger;

        public DgiiDownloader(HttpClient http, ILogger<DgiiDownloader> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<string> DescargarZipAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = "https://dgii.gov.do/app/WebApps/Consultas/RNC/RNC_CONTRIBUYENTES.zip";

                var fileName = $"dgii_{DateTime.Now:yyyyMMdd}.zip";

                var path = Path.Combine(AppContext.BaseDirectory, fileName);

                _logger.LogInformation("Descargando archivo DGII...");

                var response = await _http.GetAsync(url, cancellationToken);

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

                await stream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation($"Archivo descargado en: {path}");

                return path;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando archivo DGII");

                throw; // 👈 importante para que el worker se entere
            }
        }
    }
}