namespace AlahiaPos.Entities.Events
{
    public class InventarioMovimientoAnuladoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.InventarioMovimientoAnulado;

        public string? Motivo { get; set; }
    }
}
