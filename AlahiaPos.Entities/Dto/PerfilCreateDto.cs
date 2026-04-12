using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlahiaPos.Entities.Dto
{
    public class PerfilCreateDto
    {
        [Required]
        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Descripcion { get; set; }

        // 👇 módulos / roles del perfil
        [Required]
        public List<int> Modulos { get; set; } = new();

        public bool Activo { get; set; } = true;
    }
}
