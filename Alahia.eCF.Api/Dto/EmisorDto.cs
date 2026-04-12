using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class EmisorDto
    {
        [XmlElement("RNCEmisor")]
        public string RNCEmisor { get; set; }

        [XmlElement("RazonSocialEmisor")]
        public string RazonSocialEmisor { get; set; }

        [XmlElement("NombreComercial")]
        public string NombreComercial { get; set; }

        [XmlElement("DireccionEmisor")]
        public string DireccionEmisor { get; set; }

        [XmlElement("Municipio")]
        public string Municipio { get; set; }

        [XmlElement("Provincia")]
        public string Provincia { get; set; }

        [XmlElement("TablaTelefonoEmisor")]
        public TablaTelefonoEmisorDto TablaTelefonoEmisor { get; set; }

        [XmlElement("CorreoEmisor")]
        public string CorreoEmisor { get; set; }

        [XmlElement("WebSite")]
        public string WebSite { get; set; }

        [XmlElement("CodigoVendedor")]
        public string CodigoVendedor { get; set; }

        [XmlElement("NumeroFacturaInterna")]
        public string NumeroFacturaInterna { get; set; }

        [XmlElement("NumeroPedidoInterno")]
        public string NumeroPedidoInterno { get; set; }

        [XmlElement("ZonaVenta")]
        public string ZonaVenta { get; set; }

        [XmlElement("FechaEmision")]
        public string FechaEmision { get; set; }
    }
}