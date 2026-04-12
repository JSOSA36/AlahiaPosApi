using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ComisionesResultDto
    {
        public int IdEmpleado { get; set; }

        public string Empleados { get; set; }

        // Total generado en comisiones
        public decimal TotalComisiones { get; set; }

        // Total consumido por el lavador
        public decimal TotalConsumo { get; set; }

        // Neto real a pagar
        public decimal NetoPagar { get; set; }
    }
}
