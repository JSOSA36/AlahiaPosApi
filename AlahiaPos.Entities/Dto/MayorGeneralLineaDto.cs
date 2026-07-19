namespace AlahiaPos.Entities.Dto
{
    public class MayorGeneralLineaDto
    {
        public DateTime Fecha { get; set; }
        public string NumeroAsiento { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public string? Referencia { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal Saldo { get; set; }
    }

    public class MayorGeneralResumenDto
    {
        public int IdCuentaContable { get; set; }
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public string TipoCuenta { get; set; } = string.Empty;
        public decimal TotalDebito { get; set; }
        public decimal TotalCredito { get; set; }
        public decimal Saldo { get; set; }
        public List<MayorGeneralLineaDto> Movimientos { get; set; } = new();
    }
}
