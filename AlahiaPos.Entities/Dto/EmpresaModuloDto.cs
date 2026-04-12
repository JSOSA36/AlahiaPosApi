using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class EmpresaModuloDto
    {
        public int Id { get; set; }   // para UPDATE

        [Required]
        public int EmpresaId { get; set; }

        [Required]
        public int ModuloId { get; set; }

        public bool Activo { get; set; } = true;
    }
}
