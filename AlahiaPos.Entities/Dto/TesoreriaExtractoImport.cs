using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaExtractoImport")]
    public class TesoreriaExtractoImport
    {
        [Key]
        public int IdTesoreriaExtractoImport { get; set; }

        public int IdEmpresa { get; set; }

        public int IdCuentaFinanciera { get; set; }

        [Required, MaxLength(260)]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Formato { get; set; } = "CSV";

        [Column(TypeName = "date")]
        public DateTime? PeriodoDesde { get; set; }

        [Column(TypeName = "date")]
        public DateTime? PeriodoHasta { get; set; }

        public int? IdUsuario { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "CARGADO";

        public DateTime FechaCarga { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Observacion { get; set; }

        [MaxLength(120)]
        public string? Banco { get; set; }

        [MaxLength(80)]
        public string? NumeroCuentaBanco { get; set; }

        [MaxLength(10)]
        public string? Moneda { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SaldoInicial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SaldoFinal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDebitos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCreditos { get; set; }

        [MaxLength(64)]
        public string? HashArchivo { get; set; }

        public int? IdTesoreriaConciliacion { get; set; }

        /// <summary>Adapter de parseo que interpretó el archivo (trazabilidad).</summary>
        [MaxLength(60)]
        public string? AdapterUsado { get; set; }

        /// <summary>Advertencias de parseo serializadas como JSON (array de strings).</summary>
        public string? ParserWarnings { get; set; }

        [ForeignKey(nameof(IdCuentaFinanciera))]
        public CuentaFinanciera? CuentaFinanciera { get; set; }
    }
}
