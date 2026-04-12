using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TicketFacturaClienteDetalleDto
    {
        public decimal Cantidad { get; set; }

        public string Descripcion { get; set; }

        public decimal Precio { get; set; }
    }
}
