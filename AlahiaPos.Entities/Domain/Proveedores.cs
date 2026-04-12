
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class Proveedores:BaseEntity
    {
        [Key]
        public int IdProveedor { get; set; }

        [Column(TypeName = "VARCHAR")]

        public string? RNC { get; set; } = "";
        public string? NombreComercial { get; set; } = "";

        public string? Telefono { get; set; } = "";

        public bool IsActivo { get; set; }
        public string? Direccion { get; set; } = "";
        public string? Nota { get; set; } = "";
        public string? Email { get; set; } = "";
    }
}
