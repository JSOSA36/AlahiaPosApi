using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Sucursal")]
    public class Sucursal
    {
        [Key]
        public int IdSucursal { get; set; }

        public int IdEmpresa { get; set; }

        [Required, MaxLength(20)]
        public string Codigo { get; set; } = "PRINC";

        [Required, MaxLength(150)]
        public string Nombre { get; set; } = "Sucursal Principal";

        public bool EsPrincipal { get; set; }

        public bool Activa { get; set; } = true;

        [MaxLength(250)]
        public string? Direccion { get; set; }

        [MaxLength(40)]
        public string? Telefono { get; set; }

        [MaxLength(80)]
        public string? Municipio { get; set; }

        [MaxLength(80)]
        public string? Provincia { get; set; }

        [MaxLength(40)]
        public string? Latitude { get; set; }

        [MaxLength(40)]
        public string? Longitude { get; set; }

        [MaxLength(300)]
        public string? ApiPrint { get; set; }

        public int? IdAlmacenPrincipal { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public int? IdUsuarioCreacion { get; set; }

        [ForeignKey(nameof(IdEmpresa))]
        public Empresas? Empresa { get; set; }
    }
}
