namespace AlahiaPos.Entities.Events
{
    public class GastoRegistradoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.GastoRegistrado;

        public decimal Monto { get; set; }
        public string? TipoGasto { get; set; }
        public string? FormaPago { get; set; }
        public string? TipoCuentaFinanciera { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public int? IdCategoriaGasto { get; set; }
        /// <summary>Cuenta de gasto de la categoría; si null se usa GASTO_OPERATIVO.</summary>
        public int? IdCuentaContableGasto { get; set; }
        public string? Detalle { get; set; }
    }

    public class GastoAnuladoEvent : DomainEventBase
    {
        public override string TipoEvento => DomainEventTypes.GastoAnulado;

        public string? Motivo { get; set; }
    }
}
