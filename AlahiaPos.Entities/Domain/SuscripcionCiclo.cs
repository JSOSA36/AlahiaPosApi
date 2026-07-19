using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("SuscripcionCiclo")]
    public class SuscripcionCiclo
    {
        [Key]
        public int IdCiclo { get; set; }
        public int IdEmpresa { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public DateTime FechaGeneracion { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public int? IdPlan { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "ABIERTO";
    }
}
