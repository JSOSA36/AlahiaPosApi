namespace AlahiaPos.Entities.Events
{
    public class VentaConfirmadaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.VentaConfirmada;

        public string NumeroFactura { get; set; } = string.Empty;
        public string TipoFactura { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        /// <summary>Parte cobrada al confirmar (caja/banco).</summary>
        public decimal MontoCobrado { get; set; }
        /// <summary>Parte a crédito (CxC).</summary>
        public decimal MontoCredito { get; set; }
        public decimal CostoInventario { get; set; }
        public string? MetodoPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        /// <summary>Desglose de pagos cobrados al confirmar (multi-método). Si vacío, se usa MetodoPago/MontoCobrado.</summary>
        public List<VentaPagoParte> Pagos { get; set; } = new();
    }

    public class VentaPagoParte
    {
        public string? Metodo { get; set; }
        public decimal Monto { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public int? IdCuentaFinanciera { get; set; }
    }
}
