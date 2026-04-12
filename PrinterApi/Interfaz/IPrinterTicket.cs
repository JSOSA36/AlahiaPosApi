namespace PrinterApi.Interfaz
{
    public interface IPrinterTicket
    {
        public Task GenerateTicketLavador(int IdFacturaHeader);
        public Task GenerateTicketFacturaCliente(int idFactura);
    }
}
