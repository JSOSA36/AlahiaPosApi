using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Area:BaseEntity
    {
        [Key]
        public int IdArea { get; set; }
        public string? Nombre { get; set; }
        public bool IsActivo { get; set; }
        public int IdAreaNegocio { get; set; }
        [ForeignKey(nameof(IdAreaNegocio))]
        public AreaNegocio AreaNegocio { get; set; }
    }
}
