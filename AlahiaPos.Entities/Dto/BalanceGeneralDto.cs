namespace AlahiaPos.Entities.Dto
{
    public class BalanceGeneralSeccionDto
    {
        public string Titulo { get; set; } = string.Empty;
        public List<BalanceGeneralLineaDto> Lineas { get; set; } = new();
        public decimal Total { get; set; }
    }

    public class BalanceGeneralLineaDto
    {
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public decimal Saldo { get; set; }
    }

    public class BalanceGeneralDto
    {
        public DateTime FechaCorte { get; set; }
        public BalanceGeneralSeccionDto Activos { get; set; } = new() { Titulo = "ACTIVOS" };
        public BalanceGeneralSeccionDto Pasivos { get; set; } = new() { Titulo = "PASIVOS" };
        public BalanceGeneralSeccionDto Capital { get; set; } = new() { Titulo = "PATRIMONIO" };
        public decimal TotalActivos { get; set; }
        public decimal TotalPasivos { get; set; }
        public decimal TotalCapital { get; set; }
        public decimal TotalPasivoCapital { get; set; }
        /// <summary>Utilidad/pérdida neta acumulada hasta la fecha de corte (sin cierre).</summary>
        public decimal ResultadoEjercicio { get; set; }
        public decimal Diferencia { get; set; }
        public bool Cuadra { get; set; }
    }
}
