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
        Task ImprimirCierre(

    int idCajaCierre
);
        Task GenerateTicketBizcocho(
    int idFacturaHeader,
    int idEmpresa);
    }
}
