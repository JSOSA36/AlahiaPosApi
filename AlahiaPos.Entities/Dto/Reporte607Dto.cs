using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class Reporte607Dto
    {
        public DateTime Fecha { get; set; }

        public string? RNC { get; set; }

        public string? NCF { get; set; }

        public string? FormaPago { get; set; }

        public decimal ITBIS { get; set; }

        public decimal Total { get; set; }
    }
}

