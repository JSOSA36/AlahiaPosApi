using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionTipoTrabajo")]
    public class ProduccionTipoTrabajo
    {
        [Key]
        public int IdTipoTrabajo { get; set; }

        [Required, MaxLength(40)]
        public string Codigo { get; set; } = "";

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        [MaxLength(400)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
