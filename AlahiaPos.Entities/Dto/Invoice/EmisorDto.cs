using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class EmisorDto
    {
        public string DireccionEmisor { get; set; }
        public DateTime FechaEmision { get; set; }
        public string RazonSocialEmisor { get; set; }
        public string RNCEmisor { get; set; }
        public string NombreComercial { get; set; }
        public string Municipio { get; set; }
        public string Provincia { get; set; }
        public List<TelefonoEmisorDto> TablaTelefonoEmisor { get; set; }
        public string CorreoEmisor { get; set; }
    }
}
