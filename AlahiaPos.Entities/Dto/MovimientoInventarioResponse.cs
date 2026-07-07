using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoInventarioResponse
    {
        public bool Success { get; set; }

        public string Mensaje { get; set; }
            = string.Empty;

        public int IdMovimiento { get; set; }

        public decimal TotalCantidad { get; set; }

        public int TotalProductos { get; set; }
    }
}
