using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class UsuarioCreateDto
    {
        [Required]
        public int IdEmpresa { get; set; }

        [Required]
        public int IdEmpleado { get; set; }

        [Required]
        public int IdPerfil { get; set; }

        [Required]
        public string Correo { get; set; }

  
        public string? Password { get; set; }

        public bool Activo { get; set; }
    }

}
