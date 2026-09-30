using AlahiaBackup.Models;

namespace AlahiaBackup.Interfaces
{
    public interface ISqlBackupService
    {
        Task<BackupArtifact> RealizarBackupAsync(
            string baseDeDatos,
            DateOnly fecha,
            CancellationToken cancellationToken);

        void EliminarAntiguos(int diasRetencion);
    }
}
