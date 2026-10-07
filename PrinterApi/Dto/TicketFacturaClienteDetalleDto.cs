namespace PrinterApi.Dto
{
    public class TicketFacturaClienteDetalleDto
    {
        public decimal Cantidad { get; set; }

        public string Descripcion { get; set; }

        public decimal Precio { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Monto { get; set; }

        public decimal Itbis { get; set; }
    }
}
