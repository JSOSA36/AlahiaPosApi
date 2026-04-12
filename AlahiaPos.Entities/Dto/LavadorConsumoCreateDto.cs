using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class LavadorConsumoCreateDto
    {
        public int IdEmpleado { get; set; }

        public int IdEmpresa { get; set; }

        public string Concepto { get; set; }

        public decimal Monto { get; set; }
    }
}
