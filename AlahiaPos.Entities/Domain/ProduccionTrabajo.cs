using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionTrabajo")]
    public class ProduccionTrabajo
    {
        [Key]
        public int IdTrabajo { get; set; }

        public int IdEmpresa { get; set; }

        [Required, MaxLength(40)]
        public string TipoTrabajoCodigo { get; set; } = "";

        public int IdFlujo { get; set; }

        [Required, MaxLength(40)]
        public string CodigoEstadoActual { get; set; } = "";

        [Required, MaxLength(40)]
        public string OrigenModulo { get; set; } = "";

        [Required, MaxLength(80)]
        public string OrigenTipo { get; set; } = "";

        public int OrigenId { get; set; }

        [Required, MaxLength(120)]
        public string IdempotencyKey { get; set; } = "";

        [Required, MaxLength(60)]
        public string NumeroVisible { get; set; } = "";

        [Required, MaxLength(200)]
        public string NombreVisible { get; set; } = "";

        [MaxLength(200)]
        public string? Referencia { get; set; }

        [MaxLength(40)]
        public string? EtiquetaContexto { get; set; }

        [MaxLength(1000)]
        public string? Observacion { get; set; }

        public int? IdUsuarioSolicita { get; set; }

        [Required, MaxLength(20)]
        public string Prioridad { get; set; } = "Normal";

        public int SlaObjetivoSegundosSnapshot { get; set; }
        public int SlaAdvertenciaSegundosSnapshot { get; set; }

        [Required, MaxLength(30)]
        public string SlaModoInicioSnapshot { get; set; } = "CREACION";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaLimiteObjetivo { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public int? IdUsuarioCancelacion { get; set; }

        [MaxLength(500)]
        public string? MotivoCancelacion { get; set; }

        public bool ActivoEnTablero { get; set; } = true;

        [MaxLength(40)]
        public string? PlantillaCodigo { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        public ICollection<ProduccionTrabajoItem> Items { get; set; } = new List<ProduccionTrabajoItem>();
    }
}
