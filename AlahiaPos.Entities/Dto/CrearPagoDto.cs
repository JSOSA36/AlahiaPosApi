using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CrearPagoDto
    {
        public int IdEmpresa { get; set; }
        public decimal Monto { get; set; }
        public IFormFile Imagen { get; set; } // 🔥 IMPORTANTE
        public string? ArchivoUrl { get; set; }
    }
}
