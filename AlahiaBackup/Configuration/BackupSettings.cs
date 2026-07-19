using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaBackup.Configuration
{
    public class BackupSettings
    {
        public string RutaBackup { get; set; } = "";

        public string HoraBackup { get; set; } = "02:00";

        public int DiasRetencion { get; set; } = 15;

        public bool Comprimir { get; set; } = true;
    }
}
