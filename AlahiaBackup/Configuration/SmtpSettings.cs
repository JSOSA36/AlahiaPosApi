namespace AlahiaBackup.Configuration
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";

        public int Port { get; set; } = 587;

        public string User { get; set; } = "";

        public string Password { get; set; } = "";

        public string FromName { get; set; } = "Alahia Backup";

        public bool EnableSsl { get; set; } = true;
    }
}
