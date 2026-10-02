using Alahia.eCF.Api.Interfaces;
using AlahiaPos.DataAccess.Seguridad;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

public class XmlFirmaService : IXmlFirmaService
{
    public string FirmarXml(string xmlString, string rutaCertificado, string passwordCertificado)
    {
        // Cargar XML
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.PreserveWhitespace = false;
        xmlDoc.LoadXml(xmlString);

        // Cargar certificado P12
        using var abierto = CertificadoP12.Abrir(File.ReadAllBytes(rutaCertificado), passwordCertificado);
        X509Certificate2 cert = abierto.Certificado;

        // Crear SignedXml
        SignedXml signedXml = new SignedXml(xmlDoc);
        signedXml.SigningKey = cert.GetRSAPrivateKey();

        // Referencia al documento completo
        Reference reference = new Reference();
        reference.Uri = "";

        // Transformación enveloped
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());

        signedXml.AddReference(reference);

        // Agregar información del certificado
        KeyInfo keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        // Calcular firma
        signedXml.ComputeSignature();

        // Obtener XML de firma
        XmlElement xmlDigitalSignature = signedXml.GetXml();

        // Insertar firma en documento
        xmlDoc.DocumentElement.AppendChild(
            xmlDoc.ImportNode(xmlDigitalSignature, true)
        );

        return xmlDoc.OuterXml;
    }
}