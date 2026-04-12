using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class ParametrosConfigs : BaseEntity
    {
        [Key]
        public int IdParametrosConfig { get; set; }
        public string NombreParametro { get; set; }
        public string Valor { get; set; } 
        public string Identificador { get; set; } 
    }
}
