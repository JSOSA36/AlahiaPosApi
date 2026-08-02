namespace AlahiaPos.Entities.Events
{
    public class PagoProveedorRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.PagoProveedorRegistrado;

        public decimal Monto { get; set; }
        public string? FormaPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public int IdOrdenCompraHeader { get; set; }
    }

    public class MovimientoBancarioRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.MovimientoBancarioRegistrado;

        public string TipoMovimiento { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public decimal Monto { get; set; }
        public int? IdCuentaOrigen { get; set; }
        public int? IdCuentaDestino { get; set; }
        public string? TipoCuentaOrigen { get; set; }
        public string? TipoCuentaDestino { get; set; }
        public string? Motivo { get; set; }
    }

    public class InventarioMovimientoRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.InventarioMovimientoRegistrado;

        public string TipoMovimiento { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public decimal Monto { get; set; }
    }
}
