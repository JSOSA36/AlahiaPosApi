using AlahiaBackup.Interfaces;

namespace AlahiaBackup
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IBackupService _backupService;

        public Worker(ILogger<Worker> logger, IBackupService backupService)
        {
            _logger = logger;
            _backupService = backupService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de backup iniciado. Revisa cada minuto si ya es la hora.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _backupService.EjecutarBackupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado en el ciclo de backup.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("Servicio de backup detenido.");
        }
    }
}
