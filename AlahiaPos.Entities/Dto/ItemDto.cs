using System.Xml.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ItemDto
    {
        [XmlElement("NumeroLinea")]
        public int NumeroLinea { get; set; }

        [XmlElement("IndicadorFacturacion")]
        public int IndicadorFacturacion { get; set; }

        [XmlElement("NombreItem")]
        public string NombreItem { get; set; }

        [XmlElement("IndicadorBienoServicio")]
        public int IndicadorBienoServicio { get; set; }

        [XmlElement("CantidadItem")]
        public decimal CantidadItem { get; set; }

        [XmlElement("UnidadMedida")]
        public int UnidadMedida { get; set; }

        [XmlElement("PrecioUnitarioItem")]
        public decimal PrecioUnitarioItem { get; set; }

        [XmlElement("MontoItem")]
        public decimal MontoItem { get; set; }
    }
}