using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("CotizadorTramoDocumento")]
    public class CotizadorTramoDocumento
    {
        [Key]
        public int Id { get; set; }

        public int DesdeDocs { get; set; }

        public int? HastaDocs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CargoUSD { get; set; }

        [MaxLength(200)]
        public string? Etiqueta { get; set; }

        public int Orden { get; set; }

        public bool Activo { get; set; } = true;
    }
}
