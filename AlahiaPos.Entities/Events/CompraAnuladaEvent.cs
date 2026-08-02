namespace AlahiaPos.Entities.Events
{
    public class CompraAnuladaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.CompraAnulada;

        public string? Motivo { get; set; }
        public string? NumeroDocumento { get; set; }
    }
}
