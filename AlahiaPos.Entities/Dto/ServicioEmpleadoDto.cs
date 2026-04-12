using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ServicioEmpleadoDto
    {
        public string TipoComision { get; set; }
        public int IdFactura { get; set; }
        public string NoFactura { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int IdEmpleado { get; set; }
        public string Empleado { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string Hora { get; set; } = string.Empty;
        public int IdProducto { get; set; }
        public string Producto { get; set; } = string.Empty;
        public decimal? TotalFactura { get; set; }
        public decimal? PagadoFactura { get; set; }
        public decimal? PendienteFactura { get; set; }
        public string? TipoFactura { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }

        public decimal PorcientoComision { get; set; }
        public decimal Comision { get; set; }
    }

}
