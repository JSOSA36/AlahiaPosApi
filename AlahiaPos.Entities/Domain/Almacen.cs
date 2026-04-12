using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Almacen
    {
        [Key]
        public int IdAlmacen { get; set; }

        public string Nombre { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public bool IsActivo { get; set; }
    }
}
