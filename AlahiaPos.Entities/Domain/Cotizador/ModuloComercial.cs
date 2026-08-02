using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("ModuloComercial")]
    public class ModuloComercial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuloId { get; set; }

        [ForeignKey(nameof(ModuloId))]
        public Modulo? Modulo { get; set; }

        [Required]
        [MaxLength(80)]
        public string CategoriaComercial { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? DescripcionComercial { get; set; }

        /// <summary>BASE | AVANZADO</summary>
        [Required]
        [MaxLength(20)]
        public string Nivel { get; set; } = "BASE";

        public bool VisibleCotizador { get; set; }

        public bool ParticipaPrecio { get; set; }

        /// <summary>Escenario exploratorio configurable. No es precio oficial de mercado.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioBaseUSD { get; set; }

        public int Orden { get; set; }

        [MaxLength(60)]
        public string? Icono { get; set; }

        public bool Activo { get; set; } = true;
    }
}
