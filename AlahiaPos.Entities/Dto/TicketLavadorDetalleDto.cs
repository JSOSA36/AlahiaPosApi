using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TicketLavadorDetalleDto
    {
        public int Cantidad { get; set; }
        public string Servicio { get; set; }
        public decimal Precio { get; set; }
    }
}
