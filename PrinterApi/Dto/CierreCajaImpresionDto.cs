namespace PrinterApi.Dto
{
    public class CierreCajaImpresionDto
    {
        public string Empresa { get; set; } = "";

        public string Usuario { get; set; } = "";

        public DateTime FechaApertura { get; set; }

        public DateTime FechaCierre { get; set; }

        public decimal FondoInicial { get; set; }

        public decimal VentasBrutas { get; set; }

        public decimal TotalDescuento { get; set; }

        public decimal TotalIngresosExtra { get; set; }

        public decimal TotalGastos { get; set; }

        public decimal TotalIngresosNetos { get; set; }

        public decimal VentasEfectivo { get; set; }

        public decimal DebeHaber { get; set; }

        public decimal TotalContado { get; set; }

        public decimal Diferencia { get; set; }

        public string Observacion { get; set; } = "";

        public List<CierreCajaMetodoPagoDto> MetodosPago { get; set; } = new();

        public List<CierreCajaProductoDto> Productos { get; set; } = new();

    }
}
