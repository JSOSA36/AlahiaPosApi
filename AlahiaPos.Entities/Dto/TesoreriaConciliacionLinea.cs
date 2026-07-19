using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaConciliacionLinea")]
    public class TesoreriaConciliacionLinea
    {
        [Key]
        public int IdTesoreriaConciliacionLinea { get; set; }
        public int IdTesoreriaConciliacion { get; set; }
        public DateTime FechaMovimiento { get; set; }
        [MaxLength(250)]
        public string? Descripcion { get; set; }
        [MaxLength(100)]
        public string? ReferenciaBanco { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }
        [MaxLength(10)]
        public string TipoLinea { get; set; } = string.Empty;
        public bool Conciliado { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
    }
}
