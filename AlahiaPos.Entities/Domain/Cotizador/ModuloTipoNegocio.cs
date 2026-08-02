using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("ModuloTipoNegocio")]
    public class ModuloTipoNegocio
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuloId { get; set; }

        [ForeignKey(nameof(ModuloId))]
        public Modulo? Modulo { get; set; }

        [Required]
        public int TipoNegocioId { get; set; }

        [ForeignKey(nameof(TipoNegocioId))]
        public TipoNegocio? TipoNegocio { get; set; }

        public bool Preseleccionado { get; set; }
    }
}
