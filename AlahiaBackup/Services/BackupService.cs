using AlahiaBackup.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlahiaBackup.Services
{
    public class BackupService : IBackupService
    {
        private readonly ILogger<BackupService> _logger;
        private readonly ISqlBackupService _sqlBackupService;

        public BackupService(
            ILogger<BackupService> logger,
            ISqlBackupService sqlBackupService)
        {
            _logger = logger;
            _sqlBackupService = sqlBackupService;
        }

        public async Task EjecutarBackupAsync()
        {
            try
            {
                _logger.LogInformation("===================================");
                _logger.LogInformation("INICIANDO PROCESO DE BACKUP");
                _logger.LogInformation("===================================");

                await _sqlBackupService.RealizarBackupAsync();

                _logger.LogInformation("===================================");
                _logger.LogInformation("BACKUP FINALIZADO CORRECTAMENTE");
                _logger.LogInformation("===================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error realizando el Backup.");
            }
        }
    }
}