using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("PoliticasVersion")]
    public class PoliticasVersion
    {
        [Key]
        public int IdVersion { get; set; }

        [Required]
        [MaxLength(20)]
        public string NumeroVersion { get; set; } = "";

        [Required]
        [MaxLength(200)]
        public string Titulo { get; set; } = "";

        [Required]
        public string Contenido { get; set; } = "";

        /// <summary>Borrador | Publicada | Archivada</summary>
        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Borrador";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? IdUsuarioCreacion { get; set; }
        public DateTime? FechaPublicacion { get; set; }
        public int? IdUsuarioPublicacion { get; set; }

        [MaxLength(500)]
        public string? Notas { get; set; }
    }
}
