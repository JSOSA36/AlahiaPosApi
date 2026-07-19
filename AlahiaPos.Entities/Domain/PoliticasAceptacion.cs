using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PoliticasAceptacion")]
    public class PoliticasAceptacion
    {
        [Key]
        public int IdAceptacion { get; set; }

        public int IdVersion { get; set; }

        [ForeignKey(nameof(IdVersion))]
        public PoliticasVersion? Version { get; set; }

        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public DateTime FechaAceptacion { get; set; } = DateTime.Now;

        [MaxLength(64)]
        public string? DireccionIp { get; set; }

        [MaxLength(500)]
        public string? Navegador { get; set; }

        [MaxLength(200)]
        public string? SistemaOperativo { get; set; }

        public bool CorreoEnviado { get; set; }
        public DateTime? FechaCorreoEnviado { get; set; }
    }
}
