using System;

namespace AlahiaPos.Entities.Dto
{
    public class AlertaPagoDto
    {
        public string Tipo { get; set; } = "advertencia"; // info, advertencia, critico
        public string Mensaje { get; set; } = "";
        /// <summary>Día del calendario de cobro (30, 1, 2, 3, …).</summary>
        public int? DiaCobro { get; set; }
        /// <summary>Días restantes hasta el límite (día 3). 0 = vence hoy.</summary>
        public int? DiasRestantes { get; set; }
    }
}
