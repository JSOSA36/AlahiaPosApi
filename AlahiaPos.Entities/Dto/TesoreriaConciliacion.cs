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

        [Column(TypeName = "decimal(18,2)")]
        public decimal ToleranciaDiferencia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SaldoConciliado { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Diferencia { get; set; }

        public int? IdUsuarioReapertura { get; set; }

        public DateTime? FechaReapertura { get; set; }

        [MaxLength(500)]
        public string? MotivoReapertura { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SaldoBancoInicial { get; set; }

        public int? IdExtractoPrincipal { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
