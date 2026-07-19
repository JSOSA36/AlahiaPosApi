using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionEstacion")]
    public class ProduccionEstacion
    {
        [Key]
        public int IdEstacion { get; set; }

        public int IdEmpresa { get; set; }

        [Required, MaxLength(40)]
        public string Codigo { get; set; } = "";

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        public bool EsDespacho { get; set; }
        public bool Activa { get; set; } = true;
        public int OrdenVisual { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
