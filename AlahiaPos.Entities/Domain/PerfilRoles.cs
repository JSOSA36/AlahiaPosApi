using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class PerfilRoles 
    {
        [Key]
        public int IdPerfilRol { get; set; }
        public int IdEmpresa { get; set; }
        // 🔗 Perfil
        public int IdPerfil { get; set; }

        [ForeignKey(nameof(IdPerfil))]
        public Perfiles Perfil { get; set; }

        // 🔗 Módulo (catálogo global)
        public int IdModulo { get; set; }

        [ForeignKey(nameof(IdModulo))]
        public Modulo Modulos { get; set; }

        // 📊 Estado (por si quieres desactivar sin borrar)
        public bool Activo { get; set; } = true;
    }
}
