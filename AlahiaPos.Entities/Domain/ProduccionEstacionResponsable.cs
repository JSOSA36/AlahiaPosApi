using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionEstacionResponsable")]
    public class ProduccionEstacionResponsable
    {
        [Key]
        public int IdEstacionResponsable { get; set; }

        public int IdEstacion { get; set; }
        public int? IdUsuario { get; set; }
        public int? IdEmpleado { get; set; }
        public bool EsPrincipal { get; set; } = true;
        public DateTime? VigenteDesde { get; set; }
        public DateTime? VigenteHasta { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdEstacion))]
        public ProduccionEstacion? Estacion { get; set; }
    }
}
