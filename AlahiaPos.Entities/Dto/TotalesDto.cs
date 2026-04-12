using System.Xml.Serialization;

namespace Alahia.eCF.Api.Dto
{
    public class TotalesDto
    {
        [XmlElement("MontoGravadoTotal")]
        public decimal MontoGravadoTotal { get; set; }

        [XmlElement("MontoGravadoI1")]
        public decimal MontoGravadoI1 { get; set; }

        [XmlElement("ITBIS1")]
        public decimal ITBIS1 { get; set; }

        [XmlElement("TotalITBIS")]
        public decimal TotalITBIS { get; set; }

        [XmlElement("TotalITBIS1")]
        public decimal TotalITBIS1 { get; set; }

        [XmlElement("MontoTotal")]
        public decimal MontoTotal { get; set; }
        [XmlElement("MontoPeriodo")]
        public decimal MontoPeriodo { get; set; }
        [XmlElement("ValorPagar")]
        public decimal ValorPagar { get; set; }
    }
}