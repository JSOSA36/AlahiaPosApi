namespace AlahiaPos.Entities.Events
{
    public class NominaPagadaEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.NominaPagada;

        public decimal TotalNeto { get; set; }
        public decimal TotalBruto { get; set; }
        public decimal TotalAfp { get; set; }
        public decimal TotalSfs { get; set; }
        public decimal TotalIsr { get; set; }
        public decimal TotalPrestamos { get; set; }
        public decimal TotalAnticipos { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public string? PeriodKey { get; set; }
        public string? Detalle { get; set; }
    }
}
