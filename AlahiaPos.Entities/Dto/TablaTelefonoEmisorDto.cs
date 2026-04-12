using System.Xml.Serialization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TablaTelefonoEmisorDto
    {
        [XmlElement("TelefonoEmisor")]
        public List<string> TelefonoEmisor { get; set; }
    }
}
