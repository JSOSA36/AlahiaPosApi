using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class UnidadMedidas
    {
        [Key]
        public int IdUnidadMedida { get; set; }

        public string Descripcion { get; set; } = "";
    }
}
