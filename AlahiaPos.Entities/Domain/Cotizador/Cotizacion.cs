using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("Cotizacion")]
    public class Cotizacion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string Folio { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? TipoNegocioCodigo { get; set; }

        public int Usuarios { get; set; }

        public int Sucursales { get; set; }

        public bool UsaFacturacionElectronica { get; set; }

        public int DocumentosElectronicosMensuales { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioMensualUSD { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string SnapshotJson { get; set; } = "{}";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public ICollection<CotizacionDetalle> Detalles { get; set; } = new List<CotizacionDetalle>();

        public ICollection<CotizacionLead> Leads { get; set; } = new List<CotizacionLead>();
    }
}
