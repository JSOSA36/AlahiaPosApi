using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoInventarioDetalleRequest
    {
        public int IdProducto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal? Precio { get; set; }

        public decimal? SubTotal { get; set; }

        public string? Observacion { get; set; }
    }
}
