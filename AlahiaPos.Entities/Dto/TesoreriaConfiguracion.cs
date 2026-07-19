using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaConfiguracion")]
    public class TesoreriaConfiguracion
    {
        [Key]
        public int IdTesoreriaConfiguracion { get; set; }

        public int IdEmpresa { get; set; }

        [Required, MaxLength(20)]
        public string ModoSaldo { get; set; } = "SALDO_DISPONIBLE";

        public bool PermitirSaldoNegativo { get; set; }
        public bool RequiereConciliacionBanco { get; set; }
        public int? IdCuentaCajaGeneral { get; set; }
        public int? IdCuentaCajaChicaDefault { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime? FechaActualizacion { get; set; }
    }
}
