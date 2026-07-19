using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace AlahiaPos.DataAccess.Seguridad
{
    public static class XmlSigner
    {
        private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        private const string Sha256Digest = "http://www.w3.org/2001/04/xmlenc#sha256";

        public static string SignXml(string xml, string p12Path, string p12Password)
        {
            var cert = new X509Certificate2(
                p12Path,
                p12Password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            return SignXml(xml, cert);
        }

        public static string SignXml(string xml, byte[] p12Bytes, string p12Password)
        {
            var cert = new X509Certificate2(
                p12Bytes,
                p12Password,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            return SignXml(xml, cert);
        }

        public static string SignXml(string xml, X509Certificate2 cert)
        {
            // false: mismo approach que aceptó E31/E32 en testecf.
            var doc = new XmlDocument { PreserveWhitespace = false };
            doc.LoadXml(xml);

            var signedXml = new SignedXml(doc)
            {
                SigningKey = cert.GetRSAPrivateKey()
            };
            signedXml.SignedInfo.SignatureMethod = RsaSha256;

            // DGII "Firmado de e-CF": Enveloped + Digest SHA-256 (sin transform C14N extra).
            var reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            reference.DigestMethod = Sha256Digest;
            signedXml.AddReference(reference);

            var keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(cert));
            signedXml.KeyInfo = keyInfo;

            signedXml.ComputeSignature();
            var xmlDigitalSignature = signedXml.GetXml();
            doc.DocumentElement!.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

            // Evitar re-serializar con XmlWriter.Save (puede alterar nodos y romper verificación).
            // OuterXml del root + declaración UTF-8 explícita.
            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + doc.DocumentElement!.OuterXml;
        }

        /// <summary>
        /// Primeros 6 caracteres del SignatureValue (Código de Seguridad DGII).
        /// </summary>
        public static string? ExtractCodigoSeguridad(string xmlFirmado)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(xmlFirmado);
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
                var node = doc.SelectSingleNode("//ds:SignatureValue", nsmgr)
                            ?? doc.SelectSingleNode("//*[local-name()='SignatureValue']");
                var raw = node?.InnerText;
                if (string.IsNullOrEmpty(raw)) return null;
                // Quitar whitespace interno (base64 a veces viene con saltos de línea).
                var val = new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
                if (val.Length < 6) return val;
                return val[..6];
            }
            catch
            {
                return null;
            }
        }
    }
}
