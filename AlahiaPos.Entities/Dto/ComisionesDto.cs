using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ComisionesDto
    {
        public int IdEmpleado { get; set; }
        public string Nombre { get; set; }
        public string ProductoServicio { get; set; }
        public string NumeroDocumento { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoValor { get; set; }
        public decimal Total { get; set; }
        public decimal TotalComisiones { get; set; }
        public decimal Valor { get; set; }


    }
}
