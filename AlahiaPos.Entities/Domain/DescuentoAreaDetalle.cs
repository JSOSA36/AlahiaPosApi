using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class DescuentoAreaDetalle
    {
        [Key]
        public int IdDescuentoAreaDetalle { get; set; }

        public int IdDescuentoHeader { get; set; }

        [ForeignKey("IdDescuentoHeader")]
        public DescuentoHeader Header { get; set; }

        public int IdArea { get; set; }

        [ForeignKey("IdArea")]
        public Area Area { get; set; }
    }
}
