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

        /// <summary>
        /// Clave opcional de reintento (GUID del cobro). Si se reenvía la misma clave
        /// para la misma empresa, no se crea una segunda venta.
        /// </summary>
        public string? IdempotencyKey { get; set; }
    }
}
