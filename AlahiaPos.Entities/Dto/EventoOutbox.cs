using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("EventosOutbox")]
    public class EventoOutbox
    {
        [Key]
        public int IdEventoOutbox { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(100)]
        public string TipoEvento { get; set; } = string.Empty;

        public int? ReferenciaId { get; set; }

        [MaxLength(100)]
        public string? ReferenciaTipo { get; set; }

        [Required]
        public string Payload { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = EventoOutboxEstados.Pendiente;

        public int Intentos { get; set; }

        [MaxLength(1000)]
        public string? MensajeError { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaProcesado { get; set; }

        /// <summary>Clave de idempotencia. Activa única mientras Pendiente/Procesando.</summary>
        [MaxLength(200)]
        public string? IdempotencyKey { get; set; }

        public DateTime? LockedUntil { get; set; }

        [MaxLength(100)]
        public string? LockedBy { get; set; }
    }
}
