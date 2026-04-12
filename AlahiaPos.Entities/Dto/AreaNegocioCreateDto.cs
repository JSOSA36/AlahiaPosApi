using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class AreaNegocioCreateDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int IdEmpresa { get; set; }
    }
}
