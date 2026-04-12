using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ModuloDto
    {
        public int IdModulo { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(50)]
        public string Codigo { get; set; }

        public string Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
