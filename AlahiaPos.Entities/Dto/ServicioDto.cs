using AlahiaPos.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class ServicioDto 
    {
        public int IdServicio { get; set; }
        public int IdArea { get; set; }
        public int? IdEmpresa { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public string? Imagen {  get; set; }
        public int? DuracionMinutos { get; set; }
        public bool IsServicio { get; set; }
    }
}
