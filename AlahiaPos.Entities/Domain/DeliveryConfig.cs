using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DeliveryConfig")]
    public class DeliveryConfig
    {
        [Key]
        public int IdDeliveryConfig { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        public bool DeliveryHabilitado { get; set; }

        public bool CalculoPorDistancia { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal TarifaBase { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrecioPorKilometro { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal CostoMinimoDelivery { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal RedondeoDelivery { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? DistanciaMaximaKm { get; set; }

        [MaxLength(30)]
        public string AccionFueraDeZona { get; set; } = DeliveryFueraDeZona.SinLimite;

        [MaxLength(40)]
        public string Proveedor { get; set; } = "OSRM";

        public int MinutosCache { get; set; } = 30;

        public bool ExigirGps { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;
    }

    public static class DeliveryFueraDeZona
    {
        public const string Rechazar = "RECHAZAR";
        public const string AvisarYPermitir = "AVISAR_Y_PERMITIR";
        public const string SinLimite = "SIN_LIMITE";
    }
}
