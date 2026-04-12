using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class OrdenCompraDetalle : BaseEntity
    {
        [Key]
        public int IdOrdenCompraDetalle { get; set; }

        public int IdOrdenCompraHeader { get; set; }
        public int IdProducto { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }
        public virtual Productos? Productos { get; set; }
    }
}
