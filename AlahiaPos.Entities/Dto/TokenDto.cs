using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class TokenDto
    {
        public int IdToken { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }   // si aplica (para estilistas o administradores)
        public string Token { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }

}
