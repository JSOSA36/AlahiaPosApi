using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class DescuentoDetalle
    {
        [Key]
        public int IdDescuentoDetalle { get; set; }

        // Relación con el Header
        public int IdDescuentoHeader { get; set; }

        [ForeignKey("IdDescuentoHeader")]
        public DescuentoHeader? Header { get; set; }

        // Servicio o Producto donde aplica el descuento
        public int IdProducto { get; set; }

        [ForeignKey("IdProducto")]
        public Productos? Producto { get; set; }
    }
}
