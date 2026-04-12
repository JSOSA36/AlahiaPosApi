namespace PrinterApi.Dto
{
    public class TicketLavadorDto
    {
        public int NumeroFactura { get; set; }
        public List<TicketLavadorDetalleDto> Servicios { get; set; }
        public DateTime Fecha { get; set; }

        public string Cliente { get; set; }

        public string AtendidoPor { get; set; }

        public string Caja { get; set; }

        public int Cantidad { get; set; }

        public string Servicio { get; set; }

        public decimal Precio { get; set; }
    }
}
