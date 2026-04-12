using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class AreaServicio
    {
        [Key]
        public int IdAreaServicio { get; set; }
        public string Nombre { get; set; } = "";
        public bool IsActivo { get; set; }
    }
}
