using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class IdDocDto
    {
        public int TipoeCF { get; set; }
        public string eNCF { get; set; }
        public int TipoIngresos { get; set; }
        public int TipoPago { get; set; }
        public DateTime FechaVencimientoSecuencia { get; set; }
        public int IndicadorMontoGravado { get; set; }
    }
}
