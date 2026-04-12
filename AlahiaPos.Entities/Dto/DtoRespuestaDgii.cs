using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class DtoRespuestaDgii
    {
        public string TrackId { get; set; }
        public string Estado { get; set; } // Aceptado, Rechazado, EnProceso
        public string CodigoError { get; set; }
        public string Mensaje { get; set; }
    }
}
