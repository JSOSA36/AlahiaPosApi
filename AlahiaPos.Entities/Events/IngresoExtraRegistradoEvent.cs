namespace AlahiaPos.Entities.Events
{
    public class IngresoExtraRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.IngresoExtraRegistrado;

        public decimal Monto { get; set; }
        public string? Categoria { get; set; }
        public string? FormaPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public string? Descripcion { get; set; }
    }
}
