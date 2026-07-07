using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CierreEncargoDiaDto
    {
        // Fecha del cierre
        public DateTime Fecha { get; set; }

        // Encargos creados hoy
        public int CantidadEncargosNuevos { get; set; }

        public decimal TotalEncargosNuevos { get; set; }

        // Cobros realizados hoy
        public decimal TotalCobradoHoy { get; set; }

        // Cartera actual
        public decimal TotalPendiente { get; set; }

        public int TotalEntregados { get; set; }

        public int TotalPendientes { get; set; }

        public int TotalVencidos { get; set; }

        // Resumen por método de pago
        public List<CierreEncargoMetodoPagoDto>
            MetodosPago
        { get; set; } = new();

        // Detalle de encargos creados hoy
        public List<CierreEncargoDetalleDto>
            Encargos
        { get; set; } = new();
    }
}
