namespace AlahiaPos.Entities.Events
{
    public class IngresoExtraAnuladoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.IngresoExtraAnulado;

        public string? Motivo { get; set; }
    }
}
