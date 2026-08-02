using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("NotasCreditoAplicaciones")]
    public class NotasCreditoAplicacion
    {
        [Key]
        public int IdAplicacion { get; set; }

        public int IdNotaCredito { get; set; }

        public int? IdFacturaHeader { get; set; }

        public int? IdSaldoAFavor { get; set; }

        public int IdEmpresa { get; set; }

        public decimal MontoAplicado { get; set; }

        /// <summary>CXC_ORIGEN | SALDO_FAVOR | APLICACION_VENTA</summary>
        [MaxLength(30)]
        public string TipoAplicacion { get; set; } = "";

        public DateTime FechaAplicacion { get; set; }

        public int? IdUsuario { get; set; }
    }

    public static class NotaCreditoTipoAplicacion
    {
        public const string CxcOrigen = "CXC_ORIGEN";
        public const string SaldoFavor = "SALDO_FAVOR";
        /// <summary>Consumo de saldo a favor al pagar una venta (POS / cobro).</summary>
        public const string AplicacionVenta = "APLICACION_VENTA";
    }
}
