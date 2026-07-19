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
        public decimal CostoInventario { get; set; }
        public string? MetodoPago { get; set; }
        public int? IdCuentaFinanciera { get; set; }
    }
}
