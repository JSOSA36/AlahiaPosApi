using AlahiaPos.Entities.Dto;
using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class EncabezadoDto
    {
        [XmlElement("Version")]
        public string Version { get; set; }

        [XmlElement("IdDoc")]
        public IdDocDto IdDoc { get; set; }

        [XmlElement("Emisor")]
        public EmisorDto Emisor { get; set; }

        [XmlElement("Comprador")]
        public CompradorDto Comprador { get; set; }

        [XmlElement("Totales")]
        public TotalesDto Totales { get; set; }
    }
}