using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrinterApi.Dto
{
    public class FacturaDetallesDto
    {
        public FacturaDetallesDto() { }

        public int IdFacturaDetalle { get; set; }
        public int IdFacturaHeader { get; set; }
        public string? Comentario { get; set; } = "";
        public int IdProducto { get; set; }
        public decimal Dias { get; set; }
        public decimal Cantidad { get; set; }
        // 🔥 DESCRIPCIÓN (IMPRESIÓN / TICKET)
        public string? Descripcion { get; set; } = "";

        // 🔥 PRECIO Y TOTAL
        public decimal Precio { get; set; }
        public decimal Total { get; set; }

        // 🔥 ENCARGOS (BIZCOCHOS)
        public string? TipoMasa { get; set; }
        public string? TipoRelleno { get; set; }
        public decimal Libras { get; set; }

        // 🔥 OPCIONAL (DETALLE FINO)
        public string? NotaDetalle { get; set; }
        public int? IdEmpleadoComision { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public bool EnviadoCocina { get; set; }
        public decimal PrecioOferta { get; set; }
        public ProductosDto Productos { get; set; }
        //[NotMapped]
       
       

    }
}
