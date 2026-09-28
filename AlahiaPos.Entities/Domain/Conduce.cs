using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("ConduceHeader")]
    public class ConduceHeader
    {
        [Key]
        public int IdConduceHeader { get; set; }
        public int IdFacturaHeader { get; set; }
        public string Numero { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string? QuienEntrega { get; set; }
        public string? QuienRecibe { get; set; }
        public string? Observacion { get; set; }
        public int? IdAlmacen { get; set; }
        public int? IdUsuario { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdSucursal { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaInseccion { get; set; }
    }

    [Table("ConduceDetalle")]
    public class ConduceDetalle
    {
        [Key]
        public int IdConduceDetalle { get; set; }
        public int IdConduceHeader { get; set; }
        public int IdFacturaDetalle { get; set; }
        public int IdProducto { get; set; }
        public decimal CantidadEntregada { get; set; }
        public DateTime FechaInseccion { get; set; }
        public int IdEmpresa { get; set; }
    }
}
