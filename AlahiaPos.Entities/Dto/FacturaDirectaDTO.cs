using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaDirectaDTO
    {
        public FacturaHeaderDto Header { get; set; }
        public List<PagoDTO> Pagos { get; set; }
    }
}
