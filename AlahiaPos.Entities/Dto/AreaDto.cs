using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class AreaDto
    {
        public int IdArea { get; set; }
        public string Nombre { get; set; } = "";
        public int IdEmpresa { get; set; }
        public bool IsActivo { get; set; } = true;
        public List<ServicioDto> Servicios { get; set; } = new();
    }
}
