using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("CuentasContables")]
    public class CuentaContable
    {
        [Key]
        public int IdCuentaContable { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string TipoCuenta { get; set; } = string.Empty;

        public int? IdCuentaPadre { get; set; }

        public int Nivel { get; set; } = 1;

        public bool PermiteMovimiento { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaInseccion { get; set; } = DateTime.Now;

        [NotMapped]
        public List<CuentaContable>? Hijos { get; set; }
    }
}
