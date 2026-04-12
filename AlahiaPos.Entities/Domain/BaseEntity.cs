using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public abstract class BaseEntity
    {
        public int IdEmpresa { get; set; }
        public DateTime FechaInseccion { get; set; }
    }
}
