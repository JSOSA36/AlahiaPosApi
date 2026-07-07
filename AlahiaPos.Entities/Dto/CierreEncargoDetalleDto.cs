using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CierreEncargoDetalleDto
    {
        public int IdFacturaHeader { get; set; }

        public string NumeroDocumento { get; set; }

        public string Cliente { get; set; }

        public string Celular { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public decimal Total { get; set; }

        public decimal Abonado { get; set; }

        public decimal Pendiente { get; set; }

        public string Estado { get; set; }
    }
}
