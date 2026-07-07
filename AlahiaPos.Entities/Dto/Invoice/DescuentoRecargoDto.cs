using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class DescuentoRecargoDto
    {
        public int NumeroLinea { get; set; }
        public string TipoAjuste { get; set; }
        public string DescripcionDescuentooRecargo { get; set; }
        public string TipoValor { get; set; }
        public decimal MontoDescuentooRecargo { get; set; }
        public int IndicadorFacturacionDescuentooRecargo { get; set; }
    }
}
