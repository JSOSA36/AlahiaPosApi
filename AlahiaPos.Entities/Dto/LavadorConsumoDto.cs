using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class LavadorConsumoDto
    {
        public int IdEmpleado { get; set; }

        public string NombreLavador { get; set; }

        public DateTime Desde { get; set; }

        public DateTime Hasta { get; set; }

        public decimal TotalConsumido { get; set; }

        public decimal ComisionGenerada { get; set; }

        public decimal PagoNetoEstimado { get; set; }

        public List<LavadorConsumoDetalleDto> Consumos { get; set; }
    }
}
