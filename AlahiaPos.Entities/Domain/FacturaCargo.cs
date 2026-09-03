using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("FacturaCargo")]
    public class FacturaCargo : BaseEntity
    {
        [Key]
        public int IdFacturaCargo { get; set; }

        public int IdFacturaHeader { get; set; }

        public int? IdCargoPagoRegla { get; set; }

        [Required, MaxLength(120)]
        public string Nombre { get; set; } = "";

        [Required, MaxLength(20)]
        public string Tipo { get; set; } = "";

        public decimal Valor { get; set; }

        public decimal BaseCalculo { get; set; }

        public decimal Monto { get; set; }

        [MaxLength(100)]
        public string? MetodoPago { get; set; }
    }
}
