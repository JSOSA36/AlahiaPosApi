namespace AlahiaBackup.Configuration
{
    public class BackupSettings
    {
        public bool Enabled { get; set; } = true;

        public string[] BasesDeDatos { get; set; } = ["AlahiaPos_Prod"];

        public string RutaBackup { get; set; } = @"C:\AlahiaBackups";

        public string HoraBackup { get; set; } = "02:00";

        /// <summary>Zona de la madrugada. República Dominicana no cambia horario.</summary>
        public string ZonaHoraria { get; set; } = "Atlantic Standard Time";

        public int DiasRetencion { get; set; } = 15;

        public bool Comprimir { get; set; } = true;

        /// <summary>Tamaño máximo de cada adjunto. Gmail rechaza el mensaje cerca de 25 MB ya codificado.</summary>
        public int MaxAdjuntoMb { get; set; } = 15;

        public int MaxPartesCorreo { get; set; } = 40;

        public string CorreoDestino { get; set; } = "";

        public int Reintentos { get; set; } = 3;

        public int MinutosEntreReintentos { get; set; } = 15;
    }
}
