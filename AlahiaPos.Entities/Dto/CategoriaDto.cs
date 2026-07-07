using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CategoriaDto:BaseEntity
    {
        public int IdCategoria { get; set; }
        public string? Nombre { get; set; }
        public IFormFile? Imagen { get; set; }  // Cambiado a IFormFile
        public bool IsActiva { get; set; }
        public string? TipoOperacion { get; set; }

    }
}
