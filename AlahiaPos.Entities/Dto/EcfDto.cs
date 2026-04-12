using Alahia.eCF.Api.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AlahiaPos.Entities.Dto
{
    [XmlRoot("ECF")]
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