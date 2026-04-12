using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class ECFDetalle
    {
        [Key]
        public int IdDetalle { get; set; }

        [Required]
        public int IdECF { get; set; }

        // ======================================================
        // 📦 INFORMACIÓN DEL PRODUCTO / SERVICIO
        // ======================================================

        /// <summary>
        /// Número de línea dentro del e-CF
        /// </summary>
        public int NumeroLinea { get; set; }

        [MaxLength(50)]
        public string? CodigoProducto { get; set; }

        [Required]
        [MaxLength(300)]
        public string Descripcion { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        // ======================================================
        // 💰 IMPUESTOS Y MONTOS
        // ======================================================

        /// <summary>
        /// Monto bruto antes de descuento
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoBruto { get; set; }

        /// <summary>
        /// Descuento aplicado en esta línea
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; }

        /// <summary>
        /// Monto gravado después de descuento
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoGravado { get; set; }

        /// <summary>
        /// ITBIS aplicado en esta línea
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoITBIS { get; set; }

        /// <summary>
        /// Total final de la línea
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalLinea { get; set; }

        // ======================================================
        // 🔗 RELACIÓN
        // ======================================================

        [ForeignKey(nameof(IdECF))]
        public ECFEncabezado ECFEncabezado { get; set; } = null!;
    }
}