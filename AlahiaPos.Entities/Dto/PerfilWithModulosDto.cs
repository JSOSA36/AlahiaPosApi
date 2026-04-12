using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class PerfilWithModulosDto
    {
        public int IdPerfil { get; set; }
        public int IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }

        public List<int> Modulos { get; set; } = new();
    }
}
