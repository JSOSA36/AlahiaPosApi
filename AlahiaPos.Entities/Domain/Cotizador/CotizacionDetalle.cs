using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain.Cotizador
{
    [Table("CotizacionDetalle")]
    public class CotizacionDetalle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CotizacionId { get; set; }

        [ForeignKey(nameof(CotizacionId))]
        public Cotizacion? Cotizacion { get; set; }

        public int? ModuloId { get; set; }

        [MaxLength(50)]
        public string? CodigoModulo { get; set; }

        [MaxLength(120)]
        public string Concepto { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoUSD { get; set; }

        /// <summary>MODULO | USUARIOS | SUCURSALES | ECF | AJUSTE_PISO | PROMO</summary>
        [MaxLength(30)]
        public string TipoLinea { get; set; } = "MODULO";
    }
}
