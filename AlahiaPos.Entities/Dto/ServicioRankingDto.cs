using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ServicioRankingDto
    {
        public string NombreServicio { get; set; } = string.Empty;
        public int Veces { get; set; }
        public decimal TotalFacturado { get; set; }
    }
}
