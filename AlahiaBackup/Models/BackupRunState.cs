namespace AlahiaBackup.Models
{
    public sealed class BackupRunState
    {
        public string Fecha { get; set; } = "";

        public bool CorreoOk { get; set; }

        public bool Agotado { get; set; }

        public int Intentos { get; set; }

        public DateTime? UltimoIntentoUtc { get; set; }

        public string? UltimoError { get; set; }
    }
}
