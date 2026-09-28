using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MesasDto
    {
        public int IdMesa { get; set; }
        public string Tipo { get; set; } = "";
        public string Numero { get; set; } = "";
        public string ImagePath { get; set; } = "";
        public string Detalle { get; set; } = "";
        public bool IsActiva { get; set; }
        public string Estado { get; set; } = "";
        public int? Capacidad { get; set; }
        public string? Forma { get; set; }
        public decimal? PosX { get; set; }
        public decimal? PosY { get; set; }
        public decimal? Rotacion { get; set; }
        public decimal? Escala { get; set; }

        public int ZonaId { get; set; }
    }
}
