using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaTipoCuenta")]
    public class TesoreriaTipoCuenta
    {
        [Key]
        public int IdTesoreriaTipoCuenta { get; set; }

        [Required, MaxLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public int Orden { get; set; }
        public bool Activo { get; set; } = true;
    }
}
