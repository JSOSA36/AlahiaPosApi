using System.IO.Compression;
using AlahiaPos.DGII.Sync.Interfaces;

namespace AlahiaPos.DGII.Sync.Servicios
{
    public class DgiiExtractor : IDgiiExtractor
    {
        private readonly ILogger<DgiiExtractor> _logger;

        public DgiiExtractor(ILogger<DgiiExtractor> logger)
        {
            _logger = logger;
        }

        public string ExtraerZip(string zipPath)
        {
            try
            {
                if (!File.Exists(zipPath))
                    throw new FileNotFoundException("El archivo ZIP no existe", zipPath);

                var extractPath = Path.Combine(AppContext.BaseDirectory, "dgii_data");

                _logger.LogInformation("Extrayendo archivo DGII...");

                // 🔥 limpiar carpeta anterior
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(zipPath, extractPath);

                _logger.LogInformation($"Archivo extraído en: {extractPath}");

                return extractPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extrayendo archivo DGII");
                throw;
            }
        }
    }
}