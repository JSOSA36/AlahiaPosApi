using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    [Table("AsientosContables")]
    public class AsientoContable
    {
        [Key]
        public int IdAsientoContable { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(30)]
        public string Numero { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        [Required]
        [MaxLength(500)]
        public string Concepto { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = ContabilidadConstantes.EstadoAsientoConfirmado;

        public int? IdUsuario { get; set; }

        [MaxLength(50)]
        public string OrigenModulo { get; set; } = ContabilidadConstantes.OrigenManual;

        public int? OrigenReferenciaId { get; set; }

        public bool EsAutomatico { get; set; }

        [MaxLength(30)]
        public string TipoOperacion { get; set; } = ContabilidadTipoOperacion.Alta;

        public int? IdAsientoContableOrigen { get; set; }

        public DateTime FechaInseccion { get; set; } = DateTime.Now;

        [NotMapped]
        public List<AsientoContableDetalle>? Detalles { get; set; }
    }
}
