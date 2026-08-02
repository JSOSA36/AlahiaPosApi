namespace AlahiaPos.Entities.Events
{
    public class CompraConfirmadaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.CompraConfirmada;

        public decimal Total { get; set; }
        public decimal TotalItbis { get; set; }
        public decimal MontoInventario { get; set; }
        public decimal MontoGasto { get; set; }
        public decimal MontoActivoFijo { get; set; }
        public bool EsContado { get; set; }
        public string? FormaPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
