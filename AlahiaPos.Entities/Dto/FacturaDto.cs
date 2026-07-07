using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaDto
    {

        public string NumeroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public string MetodoPago { get; set; }
        public decimal Total { get; set; }
        public string NCF { get; set; } = "";
        public string RNC { get; set; } = "";
        public string NombreEmpresa { get; set; } = "";
    }
}
