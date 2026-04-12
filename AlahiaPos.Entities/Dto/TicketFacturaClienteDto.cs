using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TicketFacturaClienteDto
    {
        public int NumeroFactura { get; set; }

        public DateTime Fecha { get; set; }

        public string Hora { get; set; }

        public string Cliente { get; set; }

        public decimal Total { get; set; }

        public string NombreEmpresa { get; set; }

        public string TelefonoEmpresa { get; set; }

        public string DireccionEmpresa { get; set; }

        public List<TicketFacturaClienteDetalleDto> Detalles { get; set; } = new();
    }
}
