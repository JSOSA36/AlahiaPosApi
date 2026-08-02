namespace AlahiaPos.Entities.Dto
{
    public class CrearConciliacionDto
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public decimal SaldoBancoFinal { get; set; }
        public decimal? SaldoBancoInicial { get; set; }
        public decimal ToleranciaDiferencia { get; set; }
        public string? Observacion { get; set; }
        public int? IdTesoreriaExtractoImport { get; set; }
    }

    public class MarcarConciliacionMovimientosDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public List<int> IdMovimientosFinancieros { get; set; } = new();
    }

    public class RegistrarCargoInteresDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string TipoMovimiento { get; set; } = "SALIDA";
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? Observacion { get; set; }
    }

    public class ReabrirConciliacionDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ConciliacionResumenDto
    {
        public int IdTesoreriaConciliacion { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public string? NombreCuenta { get; set; }
        public DateTime PeriodoDesde { get; set; }
        public DateTime PeriodoHasta { get; set; }
        public decimal SaldoLibrosInicial { get; set; }
        public decimal SaldoLibrosFinal { get; set; }
        public decimal? SaldoBancoFinal { get; set; }
        public decimal? SaldoConciliado { get; set; }
        public decimal? Diferencia { get; set; }
        public decimal ToleranciaDiferencia { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaCierre { get; set; }
        public int MovimientosConciliados { get; set; }
        public int MovimientosPendientes { get; set; }
    }
}
