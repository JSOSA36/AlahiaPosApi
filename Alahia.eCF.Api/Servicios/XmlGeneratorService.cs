using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Alahia.eCF.Api.Dto;
using Alahia.eCF.Api.Interfaces;

namespace Alahia.eCF.Api.Services
{
    public class XmlGeneratorService : IXmlGeneratorService
    {
        public string GenerarXml(EcfDto dto)
        {
            var serializer = new XmlSerializer(typeof(EcfDto));

            var namespaces = new XmlSerializerNamespaces();

            // 🔥 Namespace correcto DGII
            namespaces.Add(string.Empty, "https://dgii.gov.do/ecf");

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false), // 🔥 SIN BOM
                Indent = true,
                OmitXmlDeclaration = false
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, dto, namespaces);
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
    }
}