using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("NotasCredito")]
    public class NotasCredito : BaseEntity
    {
        [Key]
        public int IdNotaCredito { get; set; }

        public int IdFacturaHeader { get; set; }

        public string? NumeroDocumento { get; set; }

        public string? NCF { get; set; }

        public string? NCFModificado { get; set; }

        public int? IdCliente { get; set; }

        public string? NombreCliente { get; set; }

        public string? RNC { get; set; }

        public decimal SubTotal { get; set; }

        public decimal TotalItbis { get; set; }

        public decimal Total { get; set; }

        public string? Observacion { get; set; }

        public int? IdUsuario { get; set; }

        public virtual ICollection<NotasCreditoDetalle> Detalles { get; set; }
            = new List<NotasCreditoDetalle>();
    }
}
