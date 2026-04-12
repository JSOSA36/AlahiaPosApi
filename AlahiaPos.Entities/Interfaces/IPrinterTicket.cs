using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IPrinterTicket
    {
        public Task GenerateTicketLavador(int IdFacturaHeader);
        public Task GenerateTicketFacturaCliente(int idFactura);
    }
}
