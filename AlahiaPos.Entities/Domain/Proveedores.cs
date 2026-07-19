using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Proveedores")]
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

        // Defaults fiscales futuros (Sprint A) — no foto de documento
        public string? RegimenDgii { get; set; }
        public byte? TipoIdentificacionDgii { get; set; }
        public string? ClasificacionRetencionItbisDefault { get; set; }
    }
}
