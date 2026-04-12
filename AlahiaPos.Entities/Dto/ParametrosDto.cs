using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ParametrosDto
    {
        public int IdParametro { get; set; }

        public int IdEmpresa { get; set; }

        public string? CodigoPOS { get; set; }

        public string Clave { get; set; }

        public string Valor { get; set; }

        public string? Descripcion { get; set; }
    }
}
