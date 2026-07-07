using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class RequestBizcochoEncargoDto
    {
        public FacturaHeaderDto Encargo { get; set; } = new();
        public List<PagoDTO> Pagos { get; set; } = new();
    }
}
