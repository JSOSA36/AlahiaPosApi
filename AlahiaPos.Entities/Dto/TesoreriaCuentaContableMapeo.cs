using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaCuentaContableMapeo")]
    public class TesoreriaCuentaContableMapeo
    {
        [Key]
        public int IdTesoreriaCuentaContableMapeo { get; set; }

        public int IdEmpresa { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public int IdCuentaContable { get; set; }

        [Required, MaxLength(30)]
        public string TipoMapeo { get; set; } = "PRINCIPAL";

        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(IdCuentaFinanciera))]
        public CuentaFinanciera? CuentaFinanciera { get; set; }

        [ForeignKey(nameof(IdCuentaContable))]
        public CuentaContable? CuentaContable { get; set; }
    }
}
