using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ZonasDto
    {
        public int ZonaId { get; set; }
        public string ZonaName { get; set; } = "";
        public bool Estado { get; set; }
        public int ImpresoraId { get; set; }
        public IEnumerable<Mesas>? Mesas { get; set; }
    }
}
