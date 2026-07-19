namespace AlahiaPos.Entities.Dto
{
    public class TesoreriaSaldoResumenDto
    {
        public int IdCuentaFinanciera { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string TipoCuenta { get; set; } = string.Empty;
        public string? SubtipoCodigo { get; set; }
        public string? SubtipoNombre { get; set; }
        public string Moneda { get; set; } = "DOP";
        public decimal BalanceInicial { get; set; }
        public decimal SaldoDisponible { get; set; }
        public decimal SaldoCalculado { get; set; }
        public decimal DiferenciaSaldo { get; set; }
        public bool Activa { get; set; }
        public bool EsPrincipal { get; set; }
        public int? IdCuentaContable { get; set; }
    }
}
