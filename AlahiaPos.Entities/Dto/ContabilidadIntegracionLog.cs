using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("ContabilidadIntegracionLog")]
    public class ContabilidadIntegracionLog
    {
        [Key]
        public int IdContabilidadIntegracionLog { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdEventoOutbox { get; set; }

        [MaxLength(50)]
        public string? OrigenModulo { get; set; }

        public int? OrigenReferenciaId { get; set; }

        [MaxLength(30)]
        public string? TipoOperacion { get; set; }

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = string.Empty;

        public int? IdAsientoContable { get; set; }

        [MaxLength(1000)]
        public string? Mensaje { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}
