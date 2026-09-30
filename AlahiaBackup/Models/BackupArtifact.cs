namespace AlahiaBackup.Models
{
    public sealed record BackupArtifact(
        string BaseDeDatos,
        string Archivo,
        long Bytes,
        string Sha256);
}
