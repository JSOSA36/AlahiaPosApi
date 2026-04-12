using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class DescuentoAplicadoDto
    {
        public bool Aplica { get; set; }
        public string Tipo { get; set; } = "";
        public decimal Valor { get; set; }
        public string NombreEvento { get; set; } = "";
    }
}
