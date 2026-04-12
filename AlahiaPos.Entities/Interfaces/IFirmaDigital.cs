using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IFirmaDigital
    {
        string FirmarXml(string xml, string rutaCertificado, string password);
    }
}
