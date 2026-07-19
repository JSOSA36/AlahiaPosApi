using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("SuscripcionCicloDetalle")]
    public class SuscripcionCicloDetalle
    {
        [Key]
        public int Id { get; set; }
        public int IdCiclo { get; set; }

        [MaxLength(40)]
        public string TipoLinea { get; set; } = "";

        public int? IdCargo { get; set; }
        public int? IdModulo { get; set; }

        [MaxLength(80)]
        public string? Codigo { get; set; }

        [MaxLength(200)]
        public string Nombre { get; set; } = "";

        public decimal Monto { get; set; }
    }
}
