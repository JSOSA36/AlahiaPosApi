using AlahiaBackup.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaBackup.Services
{
    public class SqlBackupService : ISqlBackupService
    {
        private readonly ILogger<SqlBackupService> _logger;

        public SqlBackupService(ILogger<SqlBackupService> logger)
        {
            _logger = logger;
        }

        public async Task RealizarBackupAsync()
        {
            _logger.LogInformation("Iniciando respaldo de bases de datos...");

            // Aquí implementaremos:
            // 1. Obtener las bases de datos.
            // 2. Crear la carpeta del día.
            // 3. Ejecutar BACKUP DATABASE.
            // 4. Verificar el archivo .bak.
            // 5. Registrar el resultado.

            await Task.CompletedTask;

            _logger.LogInformation("Respaldo finalizado.");
        }
    }
}
