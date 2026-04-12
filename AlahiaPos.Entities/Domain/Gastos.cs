using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Gastos: BaseEntity
    {
        [Key]
        public int IdGasto { get; set; }
        public string? TipoGasto { get; set; }
        public int IdProveedor { get; set; }
        public decimal Monto { get; set; }
        public string? Orien { get; set; }
        public string? Detalle { get; set; }
        public int? IdEmpleado { get; set; }
        public bool? EstaCerrada { get; set; }
        [NotMapped]
        public Proveedores? Proveedores { get; set; }
    }
}
