namespace PrinterApi.Interfaz
{
    public interface IPrinterTicket
    {
        public Task GenerateTicketLavador(int IdFacturaHeader);
        Task ImprimirCierreEncargos(
        int idEmpresa
        );
        public Task GenerateTicketFacturaCliente(int idFactura);
        public Task GenerateFactDirect(int idFactura);

        /// <summary>
        /// PDF preview del ticket 80mm (con QR e-CF). No imprime.
        /// </summary>
        Task<byte[]> PreviewTicketFacturaClientePdfAsync(int idFactura);
        Task ImprimirCierre(

    int idCajaCierre
);
        Task GenerateTicketBizcocho(
    int idFacturaHeader,
    int idEmpresa);

        Task GenerateTicketNotaCredito(
            int idNotaCredito,
            int idEmpresa);

        Task GenerateTicketReciboAbono(int idPago);
    }
}
