namespace AlahiaPos.Entities.Dto
{
    public class CierreCajaDto
    {
        // Forma de pago
        public string? FormaPago { get; set; }

        // Total por forma de pago
        public decimal Total { get; set; }

        // Ventas antes de descuentos
        public decimal TotalVentasBrutas { get; set; }
        public decimal TotalIngresosExtra { get; set; }

        // Total de descuentos aplicados
        public decimal TotalDescuento { get; set; }

        // Gastos registrados en la caja
        public decimal TotalGastos { get; set; }

        // Ventas - Descuentos - Gastos
        public decimal TotalIngresosNetos { get; set; }
    }
}