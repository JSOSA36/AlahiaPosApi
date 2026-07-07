using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoInventarioDetalleDto
    {
        public int IdProducto { get; set; }

        public string Producto { get; set; }
            = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal StockAnterior { get; set; }

        public decimal StockNuevo { get; set; }

        public decimal? Precio { get; set; }

        public decimal? SubTotal { get; set; }
    }
}
