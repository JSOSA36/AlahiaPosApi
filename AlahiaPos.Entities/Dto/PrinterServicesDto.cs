using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TicketLavadorDto
    {
        public int NumeroFactura { get; set; }
        public List<TicketLavadorDetalleDto> Servicios { get; set; }
        public DateTime Fecha { get; set; }

        public string Cliente { get; set; }

        public string AtendidoPor { get; set; }

        public string Caja { get; set; }

        public int Cantidad { get; set; }

        public string Servicio { get; set; }

        public decimal Precio { get; set; }
    }
}
