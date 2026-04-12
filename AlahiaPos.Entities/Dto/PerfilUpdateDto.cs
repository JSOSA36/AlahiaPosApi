using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PerfilUpdateDto
    {
        [Required]
        public int IdPerfil { get; set; }

        [Required]
        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activo { get; set; }

        [Required]
        public List<int> Modulos { get; set; } = new();
    }
}
