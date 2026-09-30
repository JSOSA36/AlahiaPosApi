using AlahiaBackup.Models;

namespace AlahiaBackup.Interfaces
{
    public interface IBackupEmailService
    {
        /// <summary>
        /// Envía el archivo. Devuelve false si no cupo en el correo y solo se avisó que quedó en disco.
        /// </summary>
        Task<bool> EnviarAsync(
            IReadOnlyList<BackupArtifact> archivos,
            DateOnly fecha,
            CancellationToken cancellationToken);

        Task EnviarErrorAsync(string error, int intento, int maxIntentos, CancellationToken cancellationToken);
    }
}
