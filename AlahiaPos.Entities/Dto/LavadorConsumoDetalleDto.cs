using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class LavadorConsumoDetalleDto
    {
        public int IdConsumo { get; set; }
        public DateTime Hora { get; set; }
        public bool EstaSaldado {  get; set; }
        public DateTime Fecha {  get; set; }
        public string Concepto { get; set; }

        public decimal Monto { get; set; }
    }
}
