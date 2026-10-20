using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DeliveryDistanciaCache")]
    public class DeliveryDistanciaCache
    {
        [Key]
        public int IdCache { get; set; }

        public int IdEmpresa { get; set; }

        public int IdSucursal { get; set; }

        [MaxLength(40)]
        public string Proveedor { get; set; } = "";

        [Column(TypeName = "decimal(10,7)")]
        public decimal LatitudDestino { get; set; }

        [Column(TypeName = "decimal(10,7)")]
        public decimal LongitudDestino { get; set; }

        [Column(TypeName = "decimal(7,3)")]
        public decimal LatitudClave { get; set; }

        [Column(TypeName = "decimal(7,3)")]
        public decimal LongitudClave { get; set; }

        [Column(TypeName = "decimal(7,3)")]
        public decimal OrigenLatitudClave { get; set; }

        [Column(TypeName = "decimal(7,3)")]
        public decimal OrigenLongitudClave { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        public decimal DistanciaKm { get; set; }

        public int? DuracionSegundos { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaExpiracion { get; set; }
    }
}
