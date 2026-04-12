using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace AlahiaPos.DataAccess.Seguridad
{
    public static class XmlSigner
    {
        public static string SignXml(
            string xml,
            string p12Path,
            string p12Password)
        {
            var cert = new X509Certificate2(
                p12Path,
                p12Password,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.Exportable);

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            var signedXml = new SignedXml(doc);

            signedXml.SigningKey = cert.GetRSAPrivateKey();

            var reference = new Reference();

            reference.Uri = "";

            reference.AddTransform(
                new XmlDsigEnvelopedSignatureTransform());

            reference.AddTransform(
                new XmlDsigC14NTransform());

            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();

            keyInfo.AddClause(new KeyInfoX509Data(cert));

            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();

            var xmlDigitalSignature =
                signedXml.GetXml();

            doc.DocumentElement.AppendChild(
                doc.ImportNode(xmlDigitalSignature, true));

            return doc.OuterXml;
        }
    }
}