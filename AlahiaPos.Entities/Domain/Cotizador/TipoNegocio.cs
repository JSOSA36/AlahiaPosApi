using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("TipoNegocio")]
    public class TipoNegocio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public int Orden { get; set; }

        public bool Activo { get; set; } = true;
    }
}
