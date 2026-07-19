using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaTipoDocumento")]
    public class TesoreriaTipoDocumento
    {
        [Key]
        public int IdTesoreriaTipoDocumento { get; set; }

        [Required, MaxLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Naturaleza { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
