using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaDetallesDto:BaseEntity
    {
        public FacturaDetallesDto() { }

        public int IdFacturaDetalle { get; set; }
        public int IdFacturaHeader { get; set; }
        public string? Comentario { get; set; } = "";
        public int IdProducto { get; set; }
        public decimal Dias { get; set; }
        public decimal Cantidad { get; set; }
        public int? IdEmpleadoComision { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public bool EnviadoCocina { get; set; }
        public decimal PrecioOferta { get; set; }
        //[NotMapped]
        public string NombreEmpleadoComision { get; set; }
        public virtual Productos? Productos { get; set; }
      
        public virtual FacturaHeaders? FacturaHeader { get; set; }

    }
}
