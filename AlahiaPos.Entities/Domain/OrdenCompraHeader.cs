using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class OrdenCompraHeader : BaseEntity
    {
        [Key]
        public int IdOrdenCompraHeader { get; set; }
        public string? NumeroDocumento { get; set; }
        public int? IdTipoDocumentos { get; set; }
        public int? IdTipoBienesServicios { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public int IdProveedor { get; set; }
        public string? CondicionFactura { get; set; }
        public string? Comentario { get; set; }
        public string? NCF { get; set; }
        [NotMapped]
        public virtual Proveedores? Proveedores { get; set; }
        [NotMapped]
        public virtual TipoDocumentos? TipoDocumentos { get; set; }

        public decimal TotalDescuento { get; set; }
        public string? Estado { get; set; }
        public string? TipoFacturaGasto { get; set; }
        public DateTime FechaBencimiento { get; set; }
        public bool Seleccione { get; set; }
        public bool AjustadaInventario { get; set; }
    }
}
