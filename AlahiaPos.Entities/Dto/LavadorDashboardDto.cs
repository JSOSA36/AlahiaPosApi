using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class LavadorDashboardDto
    {
        public int IdEmpleado { get; set; }
        public string NombreLavador { get; set; }

        // ⭐ total consumido en el rango
        public decimal TotalConsumido { get; set; }

        // ⭐ comisión generada en el rango
        public decimal TotalComision { get; set; }

        // ⭐ deuda pendiente real
        public decimal BalancePendiente { get; set; }

        // ⭐ pago neto estimado
        public decimal PagoNetoEstimado { get; set; }

        // ⭐ listado detalle consumos
        public List<LavadorConsumoDetalleDto> Consumos { get; set; }
            = new List<LavadorConsumoDetalleDto>();
    }
}
