using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CuentaPorCobrarDto
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public decimal TotalDeuda { get; set; }
        public int? IdEmpleados { get; set; }
        public bool EsEmpleado { get; set; }
    }
}
