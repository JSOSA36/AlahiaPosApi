namespace AlahiaPos.Entities.Dto
{
    public class EstadoCuentaClienteMovimientoDto
    {
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = ""; // FACTURA | PAGO | SALDO_INICIAL
        public int? IdDocumento { get; set; }
        public string? NumeroDocumento { get; set; }
        public string Concepto { get; set; } = "";
        public decimal Debito { get; set; }
        public decimal Credito { get; set; }
        public decimal Balance { get; set; }
        public string? FormaPago { get; set; }
        public int? IdFacturaHeader { get; set; }
    }

    public class EstadoCuentaClienteFacturaPendienteDto
    {
        public int IdFacturaHeader { get; set; }
        public DateTime Fecha { get; set; }
        public string? NumeroDocumento { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal MontoOriginal { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }
        public int? DiasVencimiento { get; set; }
        public string Estado { get; set; } = "";
    }

    public class EstadoCuentaClienteDto
    {
        public int IdEmpresa { get; set; }
        public int IdCliente { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteDocumento { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalCobrado { get; set; }
        public decimal BalancePendiente { get; set; }
        public List<EstadoCuentaClienteMovimientoDto> Movimientos { get; set; } = new();
        public List<EstadoCuentaClienteFacturaPendienteDto> FacturasPendientes { get; set; } = new();
    }
}
