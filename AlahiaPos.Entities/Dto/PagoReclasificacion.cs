using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Corrección auditada de medio de pago detectada desde conciliación bancaria.
    /// No altera la factura fiscal ni reescribe cierres históricos: solo reclasifica Caja→Banco.
    /// </summary>
    [Table("PagoReclasificacion")]
    public class PagoReclasificacion
    {
        [Key]
        public int IdPagoReclasificacion { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdTesoreriaConciliacion { get; set; }

        public int IdTesoreriaExtractoLinea { get; set; }

        [MaxLength(40)]
        public string DocumentoTipo { get; set; } = "FACTURA";

        public int? IdDocumento { get; set; }

        public int? IdFacturaHeader { get; set; }

        public int? IdPagoFacturaCliente { get; set; }

        public int? IdIngreso { get; set; }

        public int IdMovimientoOriginal { get; set; }

        public int? IdMovimientoReclasificacion { get; set; }

        public int IdCuentaOrigen { get; set; }

        public int IdCuentaDestino { get; set; }

        [MaxLength(50)]
        public string MetodoPagoOriginal { get; set; } = string.Empty;

        [MaxLength(50)]
        public string MetodoPagoEfectivo { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaMovimientoOriginal { get; set; }

        public DateTime FechaEfectiva { get; set; }

        public DateTime FechaContable { get; set; }

        /// <summary>ANTES_CIERRE | POST_CIERRE</summary>
        [MaxLength(20)]
        public string Tratamiento { get; set; } = "POST_CIERRE";

        public int? IdCajaAperturaSnapshot { get; set; }

        public int? IdCajaCierreSnapshot { get; set; }

        public bool CajaEstabaCerrada { get; set; }

        public int? IdPeriodoContableSnapshot { get; set; }

        public bool PeriodoOriginalCerrado { get; set; }

        public int IdUsuario { get; set; }

        [Required]
        [MaxLength(500)]
        public string Motivo { get; set; } = string.Empty;

        /// <summary>APLICADA | REVERSADA | PENDIENTE_CONTABLE</summary>
        [MaxLength(30)]
        public string Estado { get; set; } = "APLICADA";

        public int? IdPagoReclasificacionReversaDe { get; set; }

        public int? IdAsientoContable { get; set; }

        [MaxLength(120)]
        public string? ClaveIdempotencia { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaReversion { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }

    public static class PagoReclasificacionEstados
    {
        public const string Aplicada = "APLICADA";
        public const string Reversada = "REVERSADA";
        public const string PendienteContable = "PENDIENTE_CONTABLE";
    }

    public static class PagoReclasificacionTratamientos
    {
        public const string AntesCierre = "ANTES_CIERRE";
        public const string PostCierre = "POST_CIERRE";
    }
}
