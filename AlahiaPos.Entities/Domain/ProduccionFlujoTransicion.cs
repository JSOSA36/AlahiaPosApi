using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionFlujoTransicion")]
    public class ProduccionFlujoTransicion
    {
        [Key]
        public int IdFlujoTransicion { get; set; }

        public int IdFlujo { get; set; }

        [Required, MaxLength(40)]
        public string CodigoDesde { get; set; } = "";

        [Required, MaxLength(40)]
        public string CodigoHasta { get; set; } = "";

        public bool RequiereMotivo { get; set; }

        [MaxLength(40)]
        public string? RequierePermiso { get; set; }

        [ForeignKey(nameof(IdFlujo))]
        public ProduccionFlujo? Flujo { get; set; }
    }
}
