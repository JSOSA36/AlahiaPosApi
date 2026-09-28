using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class FacturaPrintDTO
    {
        public int IdFactura { get; set; }
        public string Cliente { get; set; }

        public string Empresa { get; set; }
        public string? NombreSucursal { get; set; }
        public string Rnc { get; set; }
        public string Direccion { get; set; }
        public string Telefono { get; set; }

        public DateTime Fecha { get; set; }
        public string TipoFactura { get; set; }

        public decimal Total { get; set; }
        public decimal Pagado { get; set; }
        public decimal Pendiente { get; set; }

        public List<FacturaItemPrintDTO> Items { get; set; }
    }
}
