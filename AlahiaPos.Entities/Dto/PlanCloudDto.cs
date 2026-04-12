using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PlanCloudDto
    {
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int Nivel { get; set; }
        public decimal LimiteFacturacion { get; set; }
        public bool EsActual { get; set; } // 🔥 CLAVE
    }
}
