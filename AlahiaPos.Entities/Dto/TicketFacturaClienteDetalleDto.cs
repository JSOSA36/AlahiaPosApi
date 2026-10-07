using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TicketFacturaClienteDetalleDto
    {
        public decimal Cantidad { get; set; }

        public string Descripcion { get; set; }

        /// <summary>Total de la línea con ITBIS. Lo siguen leyendo agentes viejos.</summary>
        public decimal Precio { get; set; }

        /// <summary>Precio unitario sin ITBIS, como la vista previa del e-CF.</summary>
        public decimal PrecioUnitario { get; set; }

        /// <summary>Cantidad × precio unitario, sin ITBIS.</summary>
        public decimal Monto { get; set; }

        /// <summary>ITBIS de la línea.</summary>
        public decimal Itbis { get; set; }
    }
}
