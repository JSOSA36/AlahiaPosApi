using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    [XmlRoot("ECF", Namespace = "https://dgii.gov.do/ecf")]
    public class EcfDto
    {
        [XmlElement("Encabezado")]
        public EncabezadoDto Encabezado { get; set; }

        [XmlArray("DetallesItems")]
        [XmlArrayItem("Item")]
        public List<ItemDto> DetallesItems { get; set; }

        [XmlElement("FechaHoraFirma")]
        public string FechaHoraFirma { get; set; }
    }
}