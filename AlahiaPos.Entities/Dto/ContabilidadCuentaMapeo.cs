using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("ContabilidadCuentaMapeo")]
    public class ContabilidadCuentaMapeo
    {
        [Key]
        public int IdContabilidadCuentaMapeo { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(50)]
        public string CodigoConcepto { get; set; } = string.Empty;

        public int IdCuentaContable { get; set; }

        public bool Activo { get; set; } = true;
    }
}
