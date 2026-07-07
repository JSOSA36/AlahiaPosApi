using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto.Invoice
{
    public class DetalleDto
    {
        public int NumeroLinea { get; set; }
        public int IndicadorFacturacion { get; set; }
        public string NombreItem { get; set; }
        public int IndicadorBienoServicio { get; set; }
        public decimal CantidadItem { get; set; }
        public decimal PrecioUnitarioItem { get; set; }
        public decimal MontoItem { get; set; }
        public int UnidadMedida { get; set; }
        public decimal TasaITBIS { get; set; }

        public decimal MontoITBIS { get; set; }
    }
}
