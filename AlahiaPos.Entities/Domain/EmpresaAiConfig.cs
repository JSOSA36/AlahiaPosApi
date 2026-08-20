using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("EmpresaAiConfig")]
    public class EmpresaAiConfig
    {
        [Key]
        public int IdEmpresa { get; set; }

        [MaxLength(40)]
        public string Provider { get; set; } = "OpenAI";

        [MaxLength(120)]
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>API key cifrada. Nunca devolver al frontend.</summary>
        public string? ApiKeyCipher { get; set; }

        [MaxLength(300)]
        public string? BaseUrl { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
        public int? IdUsuarioActualizacion { get; set; }
    }
}
