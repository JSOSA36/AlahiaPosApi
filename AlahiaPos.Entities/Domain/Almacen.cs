using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Almacenes")]
    public class Almacen
    {
        [Key]
        public int IdAlmacen { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        public bool EsPrincipal { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int? IdUsuarioCreacion { get; set; }

        public virtual ICollection<AlmacenExistencia> Existencias { get; set; }
            = new List<AlmacenExistencia>();
    }
}
