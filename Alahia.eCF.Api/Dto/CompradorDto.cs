using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class CompradorDto
    {
        [XmlElement("RNCComprador")]
        public string RNCComprador { get; set; }

        [XmlElement("RazonSocialComprador")]
        public string RazonSocialComprador { get; set; }

        [XmlElement("ContactoComprador")]
        public string ContactoComprador { get; set; }

        [XmlElement("CorreoComprador")]
        public string CorreoComprador { get; set; }

        [XmlElement("DireccionComprador")]
        public string DireccionComprador { get; set; }

        [XmlElement("MunicipioComprador")]
        public string MunicipioComprador { get; set; }

        [XmlElement("ProvinciaComprador")]
        public string ProvinciaComprador { get; set; }

        [XmlElement("FechaEntrega")]
        public string FechaEntrega { get; set; }

        [XmlElement("FechaOrdenCompra")]
        public string FechaOrdenCompra { get; set; }

        [XmlElement("NumeroOrdenCompra")]
        public string NumeroOrdenCompra { get; set; }

        [XmlElement("CodigoInternoComprador")]
        public string CodigoInternoComprador { get; set; }
    }
}