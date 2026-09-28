using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class CajaCierre
    {
        [Key]
        public int IdCajaCierre { get; set; }

        public int IdCajaApertura { get; set; }

        public int IdEmpresa { get; set; }

        public int? IdSucursal { get; set; }

        public int IdUsuario { get; set; }

        public DateTime FechaCierre { get; set; }
            = DateTime.Now;

        /* =====================================
        🔥 RESUMEN DEL CIERRE
        ===================================== */

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasBrutas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDescuento { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBillet { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIngresosExtra { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGastos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalIngresosNetos { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DebeHaber { get; set; }

        /* =====================================
        🔥 MÉTODOS DE PAGO
        ===================================== */

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEfectivo { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTarjeta { get; set; }

       

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalCredito { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGeneral { get; set; }

        /* =====================================
        🔥 RESULTADO DEL CIERRE
        ===================================== */

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoRealCaja { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Diferencia { get; set; }

        public string? Observacion { get; set; }

        /* =====================================
        🔥 RELACIÓN
        ===================================== */

        [ForeignKey(nameof(IdCajaApertura))]
        public virtual CajaApertura? CajaApertura { get; set; }
    }
}