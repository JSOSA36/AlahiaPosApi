using AlahiaPos.Entities.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class LoginResponse
    {
        public Usuarios Usuario { get; set; }
        public IEnumerable<UsuarioModulo> Modulos { get; set; }
        public string Token { get; set; }
        public bool PuedeEliminarOrden { get; set; } // 👈 NUEVO
    }
}
