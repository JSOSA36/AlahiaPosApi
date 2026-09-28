using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Cocinas : BaseEntity
    {

        [Key]
        public int IdCocina { get; set; }
        public int? IdSucursal { get; set; }
        public string Nombre { get; set; } = "";
    }
}
