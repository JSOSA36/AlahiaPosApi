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

        // Fotografía fiscal (Sprint A)
        public string? CodigoTipoComprobanteDgii { get; set; }
        public DateTime? FechaFacturaOrigen { get; set; }
        public decimal MontoGravado { get; set; }
        public decimal MontoExento { get; set; }
        public decimal MontoGravadoI1 { get; set; }
        public decimal MontoGravadoI2 { get; set; }
        public decimal MontoGravadoI3 { get; set; }
        public decimal MontoGravadoI4 { get; set; }
        public decimal DescuentoAfectaBase { get; set; }
        public decimal? TasaItbisPrincipal { get; set; }
        public byte? TipoIngresoDgii { get; set; }
        public int FotografiaFiscalVersion { get; set; }
        public string? EstadoFiscalDocumento { get; set; }

        /// <summary>Prefijo del comprobante origen (E31, E32, …).</summary>
        [MaxLength(10)]
        public string? TipoDocumentoOrigen { get; set; }

        public int? IdEcf { get; set; }

        [MaxLength(100)]
        public string? TrackId { get; set; }

        [MaxLength(50)]
        public string? EstadoDgii { get; set; }

        public DateTime? FechaEmisionEcf { get; set; }

        [MaxLength(1000)]
        public string? MensajeEmision { get; set; }

        public decimal MontoOriginal { get; set; }

        public decimal SaldoDisponible { get; set; }

        /// <summary>Activa | Anulada</summary>
        [MaxLength(20)]
        public string Estado { get; set; } = "Activa";
    }

    public static class NotaCreditoEstado
    {
        public const string Activa = "Activa";
        public const string Anulada = "Anulada";
    }
}
