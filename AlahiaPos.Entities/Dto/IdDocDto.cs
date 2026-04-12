using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class IdDocDto
    {
        [XmlElement("TipoeCF")]
        public int TipoeCF { get; set; }

        [XmlElement("eNCF")]
        public string eNCF { get; set; }

        [XmlElement("FechaVencimientoSecuencia")]
        public string FechaVencimientoSecuencia { get; set; }

        [XmlElement("IndicadorMontoGravado")]
        public int IndicadorMontoGravado { get; set; }

        [XmlElement("TipoIngresos")]
        public string TipoIngresos { get; set; }

        [XmlElement("TipoPago")]
        public int TipoPago { get; set; }
    }
}