namespace AlahiaPos.Entities.Dto
{
    public class MovimientoFinancieroFiltroDto
    {
        public int IdEmpresa { get; set; }
        public int? IdCuentaFinanciera { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string? TipoMovimiento { get; set; }
        public string? Categoria { get; set; }
        public string? Estado { get; set; }
        public string? DocumentoReferencia { get; set; }
        public string? EstadoConciliacion { get; set; }
    }

    public class MovimientoFinancieroListadoDto
    {
        public int IdMovimientoFinanciero { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int? IdCuentaOrigen { get; set; }
        public int? IdCuentaDestino { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public int? ReferenciaId { get; set; }
        public string? ReferenciaTipo { get; set; }
        public decimal Monto { get; set; }
        public string? Motivo { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string EstadoConciliacion { get; set; } = string.Empty;
        public int? IdTesoreriaConciliacion { get; set; }
        public DateTime? FechaConciliacion { get; set; }
        public string? NumeroComprobante { get; set; }
        public string? ClaveIdempotencia { get; set; }
        public decimal SaldoAcumulado { get; set; }
    }

    public class AnularMovimientoDto
    {
        public int IdMovimientoFinanciero { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class RegistrarAjusteDto
    {
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public int IdCuentaFinanciera { get; set; }
        public string TipoMovimiento { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public string? ClaveIdempotencia { get; set; }
    }

    public class EstadoCuentaDto
    {
        public int IdCuentaFinanciera { get; set; }
        public string NombreCuenta { get; set; } = string.Empty;
        public decimal SaldoInicial { get; set; }
        public decimal SaldoFinal { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public List<MovimientoFinancieroListadoDto> Movimientos { get; set; } = new();
    }
}
