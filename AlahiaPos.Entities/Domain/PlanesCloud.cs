using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class PlanesCloud
    {
        [Key]
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public int CantEquipos { get; set; }
        public decimal LimiteFacturacion { get; set; }
        public decimal PrecioUSD { get; set; }
    }
}
