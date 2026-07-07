using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PagoEncargoDto
    {
        // 🔥 ID ENCARGO
        public int IdEncargo { get; set; }
        public int IdEmpresa { get; set; }
        // 🔥 SUBTOTAL
        // MONTO ORIGINAL SIN ITBIS
        public decimal Monto { get; set; }

        // 🔥 ITBIS
        public decimal Itbis { get; set; }

        // 🔥 TOTAL PAGADO
        // MONTO + ITBIS
        public decimal TotalPago { get; set; }

        // 🔥 FORMA PAGO
        public string FormaPago { get; set; }
            = "Efectivo";

        // 🔥 COMPROBANTE
        public string? TipoComprobante { get; set; }

        // 🔥 DATOS FISCALES
        public string? RNC { get; set; }

        public string? NombreEmpresa { get; set; }
    }
}