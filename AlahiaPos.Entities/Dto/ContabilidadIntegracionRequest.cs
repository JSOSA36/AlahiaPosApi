namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Contrato para asientos automáticos desde otros módulos del ERP (Fase futura).
    /// </summary>
    public class ContabilidadIntegracionRequest
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string OrigenModulo { get; set; } = string.Empty;
        public int OrigenReferenciaId { get; set; }
        public string TipoOperacion { get; set; } = ContabilidadTipoOperacion.Alta;
        public int? IdAsientoContableOrigen { get; set; }
        public List<ContabilidadIntegracionLinea> Lineas { get; set; } = new();
    }

    public class ContabilidadIntegracionLinea
    {
        public int IdCuentaContable { get; set; }
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public string? Referencia { get; set; }
    }
}
