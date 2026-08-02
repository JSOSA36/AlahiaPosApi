using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("CotizacionLead")]
    public class CotizacionLead
    {
        [Key]
        public int Id { get; set; }

        public int? CotizacionId { get; set; }

        [ForeignKey(nameof(CotizacionId))]
        public Cotizacion? Cotizacion { get; set; }

        [Required]
        [MaxLength(30)]
        public string Tipo { get; set; } = "CONTACTO";

        [MaxLength(150)]
        public string? Nombre { get; set; }

        [MaxLength(150)]
        public string? Correo { get; set; }

        [MaxLength(40)]
        public string? Telefono { get; set; }

        [MaxLength(1000)]
        public string? Mensaje { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
