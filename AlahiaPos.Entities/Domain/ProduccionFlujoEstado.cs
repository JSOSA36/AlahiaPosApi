using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ProduccionFlujoEstado")]
    public class ProduccionFlujoEstado
    {
        [Key]
        public int IdFlujoEstado { get; set; }

        public int IdFlujo { get; set; }

        [Required, MaxLength(40)]
        public string Codigo { get; set; } = "";

        [Required, MaxLength(80)]
        public string NombreVisible { get; set; } = "";

        public int Orden { get; set; }
        public bool EsInicial { get; set; }
        public bool EsTerminal { get; set; }
        public bool CuentaParaCompletar { get; set; } = true;

        [MaxLength(40)]
        public string? Icono { get; set; }

        [MaxLength(20)]
        public string? ColorHint { get; set; }

        [ForeignKey(nameof(IdFlujo))]
        public ProduccionFlujo? Flujo { get; set; }
    }
}
