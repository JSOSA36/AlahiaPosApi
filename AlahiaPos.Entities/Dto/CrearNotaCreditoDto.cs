using System.Collections.Generic;

namespace AlahiaPos.Entities.Dto
{
    public class CrearNotaCreditoDto
    {
        public int IdFacturaHeader { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public string? Observacion { get; set; }

        public List<CrearNotaCreditoLineaDto> Lineas { get; set; }
            = new();
    }

    public class CrearNotaCreditoLineaDto
    {
        public int IdFacturaDetalle { get; set; }

        public decimal Cantidad { get; set; }
    }

    /// <summary>
    /// Nota de crédito comercial (no devolución): concepto + monto.
    /// No mueve inventario ni altera cantidades devueltas de la factura.
    /// </summary>
    public class CrearNotaCreditoComercialDto
    {
        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        /// <summary>
        /// Opcional. En e-CF el receptor suele venir solo como RNC/nombre en la factura;
        /// no se exige cliente del catálogo.
        /// </summary>
        public int? IdCliente { get; set; }

        /// <summary>Opcional. Si se indica, la NC queda asociada a esa factura origen.</summary>
        public int? IdFacturaHeader { get; set; }

        public string Concepto { get; set; } = "";

        /// <summary>Monto total de la NC (incluye ITBIS si MontoItbis se informa o se deriva).</summary>
        public decimal Monto { get; set; }

        /// <summary>ITBIS incluido en Monto. Si null y hay factura, se deriva proporcional; si no, 0.</summary>
        public decimal? MontoItbis { get; set; }
    }

    public class AnularNotaCreditoDto
    {
        public int IdNotaCredito { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public string? Motivo { get; set; }

        public string? UsuarioAnulo { get; set; }
    }
}
