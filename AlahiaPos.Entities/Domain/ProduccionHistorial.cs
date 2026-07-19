using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionHistorial")]
    public class ProduccionHistorial
    {
        [Key]
        public int IdHistorial { get; set; }

        public int IdTrabajo { get; set; }
        public int? IdTrabajoItem { get; set; }

        [MaxLength(40)]
        public string? CodigoEstadoAnterior { get; set; }

        [Required, MaxLength(40)]
        public string CodigoEstadoNuevo { get; set; } = "";

        public int? IdUsuario { get; set; }
        public int? IdEstacion { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Motivo { get; set; }

        [Required, MaxLength(40)]
        public string Origen { get; set; } = "Sistema";
    }
}
