namespace PrinterApi.Dto
{
    public class TicketLavadorDto
    {
        public int NumeroFactura { get; set; }
        public List<TicketLavadorDetalleDto> Servicios { get; set; }
        public DateTime Fecha { get; set; }

        public string Cliente { get; set; }

        public string AtendidoPor { get; set; }

        public string NombreEmpresa { get; set; }

        public string NombreSucursal { get; set; }

        public string DireccionEmpresa { get; set; }

        public string TelefonoEmpresa { get; set; }

        public string RncEmpresa { get; set; }

        public string Caja { get; set; }

        public int Cantidad { get; set; }

        public string Servicio { get; set; }

        public decimal Precio { get; set; }
    }
}
