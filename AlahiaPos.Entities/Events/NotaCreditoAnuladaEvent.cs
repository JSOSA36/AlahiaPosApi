namespace AlahiaPos.Entities.Events
{
    public class NotaCreditoAnuladaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.NotaCreditoAnulada;

        public string? Motivo { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
