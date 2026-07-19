namespace AlahiaPos.Entities.Dto
{
    public class BalanceComprobacionLineaDto
    {
        public int IdCuentaContable { get; set; }
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; }
        public decimal TotalDebito { get; set; }
        public decimal TotalCredito { get; set; }
        public decimal SaldoFinal { get; set; }
    }

    public class BalanceComprobacionResumenDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<BalanceComprobacionLineaDto> Lineas { get; set; } = new();
        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }
    }
}
