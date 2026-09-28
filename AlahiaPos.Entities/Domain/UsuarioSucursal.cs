using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("UsuarioSucursal")]
    public class UsuarioSucursal
    {
        [Key]
        public int IdUsuarioSucursal { get; set; }

        public int IdUsuario { get; set; }

        public int IdSucursal { get; set; }

        public bool EsDefault { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdUsuario))]
        public Usuarios? Usuario { get; set; }

        [ForeignKey(nameof(IdSucursal))]
        public Sucursal? Sucursal { get; set; }
    }
}
