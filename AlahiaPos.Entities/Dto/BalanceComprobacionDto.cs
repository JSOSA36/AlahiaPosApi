namespace AlahiaPos.Entities.Dto
{
    public class BalanceComprobacionLineaDto
    {
        public int IdCuentaContable { get; set; }
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        /// <summary>Saldo inicial según naturaleza de la cuenta.</summary>
        public decimal SaldoInicial { get; set; }
        public decimal TotalDebito { get; set; }
        public decimal TotalCredito { get; set; }
        /// <summary>Saldo final según naturaleza de la cuenta.</summary>
        public decimal SaldoFinal { get; set; }
        public decimal SaldoDeudor { get; set; }
        public decimal SaldoAcreedor { get; set; }
    }

    public class BalanceComprobacionResumenDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<BalanceComprobacionLineaDto> Lineas { get; set; } = new();
        public decimal TotalDebitos { get; set; }
        public decimal TotalCreditos { get; set; }
        public decimal TotalSaldoDeudor { get; set; }
        public decimal TotalSaldoAcreedor { get; set; }
        public bool Cuadra { get; set; }
    }
}
