using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Mesas
    {
        [Key]
        public int IdMesa { get; set; }
        public string? Tipo { get; set; } = "";
        public string? Numero { get; set; } = "";
        public string? ImagePath { get; set; } = "";
        public string? Detalle { get; set; } = "";
        public bool IsActiva { get; set; }  
        public string? Estado { get; set; } = "";
        public DateTime FechaInseccion { get; set; }
        /// <summary>Sillas / cubiertos. Fuente de la escena visual del salón.</summary>
        [NotMapped]
        public int? Capacidad { get; set; }
        /// <summary>round | square | rect. Para el plano visual.</summary>
        [NotMapped]
        public string? Forma { get; set; }
        [NotMapped]
        public decimal? PosX { get; set; }
        [NotMapped]
        public decimal? PosY { get; set; }
        [NotMapped]
        public decimal? Rotacion { get; set; }
        [NotMapped]
        public decimal? Escala { get; set; }
       
        public int ZonaId { get; set; }
        [ForeignKey("ZonaId")]
        public virtual Zonas? zonas { get; set; }
    }
}
