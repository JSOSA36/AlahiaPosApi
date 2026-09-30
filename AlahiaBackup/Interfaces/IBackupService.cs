namespace AlahiaBackup.Interfaces
{
    public interface IBackupService
    {
        Task EjecutarBackupAsync(CancellationToken cancellationToken);
    }
}
