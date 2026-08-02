namespace AlahiaPos.Entities.Events
{
    public class NotaCreditoCreadaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.NotaCreditoCreada;

        public int IdFacturaHeader { get; set; }
        public string? NumeroDocumento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        /// <summary>Parte que reduce CxC.</summary>
        public decimal MontoCxc { get; set; }
        /// <summary>Parte de devolución/reembolso a caja/banco.</summary>
        public decimal MontoTesoreria { get; set; }
        public decimal CostoInventario { get; set; }
        public string? MetodoPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
    }
}
