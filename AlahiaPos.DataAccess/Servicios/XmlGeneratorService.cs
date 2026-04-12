using Alahia.eCF.Api.Dto;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AlahiaPos.DataAccess.Servicios
{
    public class XmlGeneratorService : IXmlGeneratorService
    {
        public string GenerarXml(EcfDto dto)
        {
            var serializer = new XmlSerializer(typeof(EcfDto));

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), // 🔥 SIN BOM (IMPORTANTE)
                Indent = false,
                OmitXmlDeclaration = true
            };

            using var stringWriter = new Utf8StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);

            // 🔥 Quitar namespaces basura
            var ns = new XmlSerializerNamespaces();
            ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
            ns.Add("xsd", "http://www.w3.org/2001/XMLSchema");

            serializer.Serialize(xmlWriter, dto, ns);

            return stringWriter.ToString();
        }
    }

    // 🔥 NECESARIO PARA UTF8 REAL
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }
}