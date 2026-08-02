using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("TesoreriaConciliacionAuditoria")]
    public class TesoreriaConciliacionAuditoria
    {
        [Key]
        public int IdTesoreriaConciliacionAuditoria { get; set; }
        public int IdTesoreriaConciliacion { get; set; }
        public int IdEmpresa { get; set; }
        public int? IdUsuario { get; set; }
        [Required, MaxLength(40)]
        public string Accion { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Detalle { get; set; }
        public int? IdTesoreriaExtractoLinea { get; set; }
        public int? IdMovimientoFinanciero { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
