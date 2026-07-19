namespace AlahiaPos.Entities.Dto
{
    public class LibroDiarioLineaDto
    {
        public int IdAsientoContable { get; set; }
        public string Numero { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public string? Referencia { get; set; }
        public string OrigenModulo { get; set; } = string.Empty;
    }
}
