using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("EmpresaCargoRecurrente")]
    public class EmpresaCargoRecurrente
    {
        [Key]
        public int Id { get; set; }
        public int IdEmpresa { get; set; }

        [MaxLength(40)]
        public string TipoCargo { get; set; } = "OTRO";

        public int? IdModulo { get; set; }

        [MaxLength(80)]
        public string Codigo { get; set; } = "";

        [MaxLength(200)]
        public string Nombre { get; set; } = "";

        public decimal MontoMensual { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.Now;
        public DateTime? FechaFin { get; set; }
        public bool Activo { get; set; } = true;

        [MaxLength(500)]
        public string? Observacion { get; set; }

        public int? IdUsuarioCreacion { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public int? IdUsuarioModificacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }
}
