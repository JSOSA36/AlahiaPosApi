using AlahiaPos.DGII.Sync.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.DGII.Sync.Servicios
{
    public class DgiiSyncService : IDgiiSyncService
    {
        private readonly IDgiiDownloader _downloader;
        private readonly IDgiiExtractor _extractor;
        private readonly IDgiiProcessor _processor;
        private readonly ILogger<DgiiSyncService> _logger;

        public DgiiSyncService(
            IDgiiDownloader downloader,
            IDgiiExtractor extractor,
            IDgiiProcessor processor,
            ILogger<DgiiSyncService> logger)
        {
            _downloader = downloader;
            _extractor = extractor;
            _processor = processor;
            _logger = logger;
        }

        public async Task EjecutarSyncAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("🚀 Iniciando sincronización DGII...");

                // 🔥 1. Descargar ZIP
                var zipPath = await _downloader.DescargarZipAsync(cancellationToken);

                // 🔥 2. Extraer
                var carpeta = _extractor.ExtraerZip(zipPath);

                // 🔥 3. Procesar CSV
                await _processor.ProcesarArchivoAsync(carpeta, cancellationToken);

                _logger.LogInformation("✅ Sincronización DGII completada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en sincronización DGII");
                throw;
            }
        }
    }
}
