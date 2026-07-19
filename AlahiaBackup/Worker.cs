using AlahiaBackup.Interfaces;

namespace AlahiaBackup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBackupService _backupService;

        public Worker(
            ILogger<Worker> logger,
            IBackupService backupService)
        {
            _logger = logger;
            _backupService = backupService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de Backup iniciado.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _backupService.EjecutarBackupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocurrió un error ejecutando el servicio de Backup.");
                }

                // Espera 1 minuto antes de volver a verificar
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Servicio de Backup detenido.");
        }
    }
}