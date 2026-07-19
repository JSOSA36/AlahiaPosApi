using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("SuscripcionEvento")]
    public class SuscripcionEvento
    {
        [Key]
        public int IdEvento { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdCiclo { get; set; }

        [MaxLength(60)]
        public string Tipo { get; set; } = "";

        public string? Detalle { get; set; }

        [MaxLength(40)]
        public string? Canal { get; set; }

        public int? IdUsuario { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
