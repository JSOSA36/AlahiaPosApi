using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class TablaTelefonoEmisorDto
    {
        [XmlElement("TelefonoEmisor")]
        public string TelefonoEmisor { get; set; }
    }
}
