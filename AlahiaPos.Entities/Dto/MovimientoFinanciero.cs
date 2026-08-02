using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MovimientoFinanciero
    {
        [Key]
        public int IdMovimientoFinanciero { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public int? IdCuentaOrigen { get; set; }

        public int? IdCuentaDestino { get; set; }

        [Required]
        [MaxLength(50)]
        public string TipoMovimiento { get; set; }
            = string.Empty;
        /*
            ENTRADA
            SALIDA
            TRANSFERENCIA
        */

        [MaxLength(100)]
        public string? Categoria { get; set; }
        /*
            VENTA
            GASTO
            PAGO_SUPLIDOR
            NOMINA
            AJUSTE
        */

        public int? ReferenciaId { get; set; }

        [MaxLength(50)]
        public string? ReferenciaTipo { get; set; }
        /*
            FACTURA
            GASTO
            CXC
            CXP
        */

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        /* =========================================
        🔥 BALANCES
        ========================================= */

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceAnteriorOrigen { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceNuevoOrigen { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceAnteriorDestino { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceNuevoDestino { get; set; }

        /* =========================================
        🔥 INFO
        ========================================= */

        [MaxLength(250)]
        public string? Motivo { get; set; }

        public string? Observacion { get; set; }

        public DateTime FechaMovimiento { get; set; }
            = DateTime.Now;

        public int? IdMovimientoPar { get; set; }

        [MaxLength(50)]
        public string? NumeroComprobante { get; set; }

        [MaxLength(20)]
        public string Estado { get; set; } = "CONFIRMADO";

        [MaxLength(20)]
        public string EstadoConciliacion { get; set; } = "PENDIENTE";

        public int? IdTesoreriaConciliacion { get; set; }

        public DateTime? FechaConciliacion { get; set; }

        public int? IdUsuarioConciliacion { get; set; }

        [MaxLength(120)]
        public string? ClaveIdempotencia { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        public int? IdTesoreriaTipoDocumento { get; set; }

        /* =========================================
        🔥 RELACIONES
        ========================================= */

        [ForeignKey(nameof(IdCuentaOrigen))]
        public CuentaFinanciera? CuentaOrigen { get; set; }

        [ForeignKey(nameof(IdCuentaDestino))]
        public CuentaFinanciera? CuentaDestino { get; set; }

        [ForeignKey(nameof(IdMovimientoPar))]
        public MovimientoFinanciero? MovimientoPar { get; set; }

        [ForeignKey(nameof(IdTesoreriaTipoDocumento))]
        public TesoreriaTipoDocumento? TesoreriaTipoDocumento { get; set; }
    }
}
