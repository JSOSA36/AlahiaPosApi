namespace AlahiaPos.Entities.Events
{
    public class VentaAnuladaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.VentaAnulada;

        public string? Motivo { get; set; }
        public string? NumeroFactura { get; set; }
    }
}
