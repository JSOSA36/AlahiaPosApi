using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class AlertaPagoDto
    {
        public string Tipo { get; set; } // info, advertencia, critico
        public string Mensaje { get; set; }
    }
}
