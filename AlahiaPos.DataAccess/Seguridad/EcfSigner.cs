using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Seguridad
{
    public class EcfSigner : IECFSigner
    {
        public string Firmar(string xml, string rutaCertificado, string passwordCertificado)
            => XmlSigner.SignXml(xml, rutaCertificado, passwordCertificado);
    }
}
