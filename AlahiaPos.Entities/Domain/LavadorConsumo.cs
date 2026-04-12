using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("LavadorConsumo")]
    public class LavadorConsumo
    {
        [Key]
        public int IdConsumo { get; set; }

        public int IdEmpleado { get; set; }

        public int IdEmpresa { get; set; }

        public DateTime Fecha { get; set; }

        public string Concepto { get; set; } = "";

        public decimal Monto { get; set; }

        // ⭐ ESTA ES LA QUE TIENES EN SQL
        public bool Pagado { get; set; }

        [ForeignKey(nameof(IdEmpleado))]
        public virtual Empleados? Empleado { get; set; }
    }
}