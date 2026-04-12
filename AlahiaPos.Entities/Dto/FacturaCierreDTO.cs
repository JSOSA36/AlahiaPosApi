using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaCierreDTO
    {
        public int IdFactura { get; set; }

        // 🔹 Contado o Crédito
        public string TipoFactura { get; set; }

        public int IdCliente { get; set; }
        public bool imprimirFactura { get; set; }

        // 🔹 PAGOS (Contado)
        public List<PagoDTO>? DetallePagos { get; set; }

        // 🔹 ABONOS (Crédito)
        public List<PagoDTO>? DetalleAbono { get; set; }
    }
}
