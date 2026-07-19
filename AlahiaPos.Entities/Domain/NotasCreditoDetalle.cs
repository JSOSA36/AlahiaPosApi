using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("NotasCreditoDetalle")]
    public class NotasCreditoDetalle
    {
        [Key]
        public int IdNotaCreditoDetalle { get; set; }

        public int IdNotaCredito { get; set; }

        public int IdFacturaDetalle { get; set; }

        public int IdProducto { get; set; }

        public string? NombreProducto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Itbis { get; set; }

        public decimal SubTotal { get; set; }

        // Fotografía fiscal línea NC (Sprint A)
        public decimal? TasaItbis { get; set; }
        public decimal MontoGravadoLinea { get; set; }
        public decimal MontoExentoLinea { get; set; }
        public decimal DescuentoAfectaBase { get; set; }
        public decimal ItbisCalculado { get; set; }

        [ForeignKey(nameof(IdNotaCredito))]
        public virtual NotasCredito? NotaCredito { get; set; }
    }
}
