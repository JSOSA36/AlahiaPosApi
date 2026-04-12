using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class IngresosPorLineaNegocioDto
    {
        public int IdAreaNegocio { get; set; }
        public string AreaNegocio { get; set; } = "";
        public string MetodoPago { get; set; } = "";
        public decimal Total { get; set; }
    }
}
