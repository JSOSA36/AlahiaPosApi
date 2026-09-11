using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("CertecfCaso")]
    public class CertecfCaso
    {
        [Key]
        public int IdCaso { get; set; }

        public int IdSesion { get; set; }

        public int Orden { get; set; }

        public int Oleada { get; set; } = 1;

        public int TipoEcf { get; set; }

        [MaxLength(20)]
        public string Encf { get; set; } = "";

        /// <summary>DATOS | ACECF | SIMULACION</summary>
        [MaxLength(20)]
        public string TipoPrueba { get; set; } = "DATOS";

        [MaxLength(30)]
        public string Estado { get; set; } = "Pendiente";

        [MaxLength(80)]
        public string? TrackId { get; set; }

        [Required]
        public string PayloadJson { get; set; } = "";

        [MaxLength(2000)]
        public string? Mensaje { get; set; }

        /// <summary>Cuerpo crudo o resumen de la última respuesta DGII (XML/JSON).</summary>
        public string? RespuestaDgii { get; set; }

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaRespuesta { get; set; }

        public CertecfSesion Sesion { get; set; } = null!;
    }
}
