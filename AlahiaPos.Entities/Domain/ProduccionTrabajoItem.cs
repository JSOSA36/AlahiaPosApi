using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionTrabajoItem")]
    public class ProduccionTrabajoItem
    {
        [Key]
        public int IdTrabajoItem { get; set; }

        public int IdTrabajo { get; set; }
        public int? OrigenDetalleId { get; set; }
        public int? IdEstacion { get; set; }

        [MaxLength(40)]
        public string? CodigoItem { get; set; }

        [Required, MaxLength(200)]
        public string NombreItem { get; set; } = "";

        public decimal Cantidad { get; set; } = 1;

        [MaxLength(500)]
        public string? Observacion { get; set; }

        [MaxLength(1000)]
        public string? VariacionesTexto { get; set; }

        [Required, MaxLength(40)]
        public string CodigoEstado { get; set; } = "";

        public int Orden { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaListo { get; set; }
        public int? IdUsuarioUltimoCambio { get; set; }
        public int? IdResponsableAsignado { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [ForeignKey(nameof(IdTrabajo))]
        public ProduccionTrabajo? Trabajo { get; set; }
    }
}
