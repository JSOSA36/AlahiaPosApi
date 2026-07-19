using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("OrdenCompraDetalles")]
    public class OrdenCompraDetalle : BaseEntity
    {
        [Key]
        public int IdOrdenCompraDetalle { get; set; }

        public int IdOrdenCompraHeader { get; set; }
        public int IdProducto { get; set; }

        /// <summary>Snapshot del TipoComportamiento del producto al guardar la línea.</summary>
        public string? TipoComportamientoLinea { get; set; }

        public decimal Cantidad { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal SubTotal { get; set; }

        /// <summary>Cantidad ya recibida físicamente vía MovimientoInventario.</summary>
        public decimal CantidadRecibida { get; set; }

        public int? IdGastoGenerado { get; set; }
        public int? IdActivoFijoGenerado { get; set; }

        // Foto fiscal línea compra (Sprint A)
        public decimal? TasaItbis { get; set; }
        public byte? DestinoItbis { get; set; }
        public decimal ItbisCalculado { get; set; }

        [NotMapped]
        public decimal CantidadPendienteRecepcion =>
            Math.Max(0, Cantidad - CantidadRecibida);

        [NotMapped]
        public virtual Productos? Productos { get; set; }
    }
}
