namespace AlahiaPos.Entities.Events
{
    public class CobroClienteRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.CobroClienteRegistrado;

        public decimal Monto { get; set; }
        public string? FormaPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public int IdFacturaHeader { get; set; }
    }
}
