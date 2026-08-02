using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaExtractoLinea")]
    public class TesoreriaExtractoLinea
    {
        [Key]
        public int IdTesoreriaExtractoLinea { get; set; }

        public int IdTesoreriaExtractoImport { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaMovimiento { get; set; }

        [MaxLength(250)]
        public string? Descripcion { get; set; }

        [MaxLength(100)]
        public string? Referencia { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Debito { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Credito { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Balance { get; set; }

        [MaxLength(20)]
        public string EstadoMatch { get; set; } = "PENDIENTE";

        public int? IdMovimientoFinanciero { get; set; }

        [Column(TypeName = "decimal(9,4)")]
        public decimal? ScoreSugerido { get; set; }

        [MaxLength(300)]
        public string? Observacion { get; set; }

        [MaxLength(30)]
        public string? AccionTomada { get; set; }

        [MaxLength(50)]
        public string? CategoriaSugerida { get; set; }

        public bool EsAutoConciliado { get; set; }

        public DateTime? FechaResolucion { get; set; }

        public int? IdUsuarioResolucion { get; set; }

        [MaxLength(80)]
        public string? ReglaMatch { get; set; }

        [MaxLength(500)]
        public string? ExplicacionMatch { get; set; }

        /// <summary>BANCARIO_PURO | OPERATIVO | DESCONOCIDO</summary>
        [MaxLength(30)]
        public string? ClasificacionLinea { get; set; }

        [MaxLength(40)]
        public string? ModuloOrigenSugerido { get; set; }

        [ForeignKey(nameof(IdTesoreriaExtractoImport))]
        public TesoreriaExtractoImport? ExtractoImport { get; set; }

        [ForeignKey(nameof(IdMovimientoFinanciero))]
        public MovimientoFinanciero? MovimientoFinanciero { get; set; }
    }
}
