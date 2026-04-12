using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrinterServices.Interfaces
{
    public interface IPrinter
    {
        Task GenerateTicketLavador(int idEmpresa);
    }
}
