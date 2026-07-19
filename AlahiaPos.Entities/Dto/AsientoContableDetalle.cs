using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("AsientosContablesDetalle")]
    public class AsientoContableDetalle
    {
        [Key]
        public int IdAsientoContableDetalle { get; set; }

        public int IdAsientoContable { get; set; }

        public int IdCuentaContable { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debito { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credito { get; set; }

        [MaxLength(200)]
        public string? Referencia { get; set; }

        [NotMapped]
        public string? CodigoCuenta { get; set; }

        [NotMapped]
        public string? NombreCuenta { get; set; }
    }
}
