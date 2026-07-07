using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CajaProductoDto
    {
        public int IdProducto { get; set; }

        public string Producto { get; set; }

        public decimal CantidadVendida { get; set; }

        public decimal ExistenciaActual { get; set; }

        public decimal TotalVendido { get; set; }
    }
}
