using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("DgiiConfiguracionAuditoria")]
    public class DgiiConfiguracionAuditoria
    {
        [Key]
        public int IdAuditoria { get; set; }
        public int IdEmpresa { get; set; }
        public int IdUsuario { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        [MaxLength(40)]
        public string Accion { get; set; } = "UPSERT";
        public string? ValorAnteriorJson { get; set; }
        public string? ValorNuevoJson { get; set; }
        [MaxLength(500)]
        public string? Motivo { get; set; }
    }
}
