using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaConciliacion")]
    public class TesoreriaConciliacion
    {
        [Key]
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoLibrosInicial { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoLibrosFinal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SaldoBancoFinal { get; set; }
        [MaxLength(20)]
        public string Estado { get; set; } = "BORRADOR";
        public int? IdUsuario { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaCierre { get; set; }
        [MaxLength(500)]
        public string? Observacion { get; set; }
    }
}
