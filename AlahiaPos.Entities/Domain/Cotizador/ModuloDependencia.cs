using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("ModuloDependencia")]
    public class ModuloDependencia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuloId { get; set; }

        [ForeignKey(nameof(ModuloId))]
        public Modulo? Modulo { get; set; }

        [Required]
        public int ModuloRequeridoId { get; set; }

        [ForeignKey(nameof(ModuloRequeridoId))]
        public Modulo? ModuloRequerido { get; set; }

        /// <summary>REQUIERE | RECOMIENDA</summary>
        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = "RECOMIENDA";

        [MaxLength(400)]
        public string? Mensaje { get; set; }

        public bool Activo { get; set; } = true;
    }
}
