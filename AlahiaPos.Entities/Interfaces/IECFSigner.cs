using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IECFSigner
    {
        string Firmar(string xml, string rutaCertificado, string passwordCertificado);
    }
}
