using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    [Table("Ingresos")]
    public class Ingresos
    {
        [Key]
        public int IdIngreso { get; set; }

        [Required]
        public int IdEmpresa { get; set; }

        // 📅 Datos principales
        [Required]
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string Descripcion { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Categoria { get; set; } = string.Empty; // Ej: "Abono a Factura", "Contado", "Arrendamiento", etc.

        [StringLength(150)]
        public string? Origen { get; set; } = string.Empty; // Ej: Cliente, proveedor, etc.

        // 💵 Datos financieros
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal Monto { get; set; }

        [StringLength(50)]
        public string FormaPago { get; set; } = "Efectivo"; // Efectivo, Transferencia, Tarjeta, Cheque...

        [StringLength(100)]
        public string Referencia { get; set; } = string.Empty; // número de comprobante o referencia de pago

        public int? IdUsuario { get; set; } // quien registró el ingreso

        // 🧾 Relaciones opcionales
        public int? IdFacturaHeader { get; set; } // si viene de una factura
        public int? IdCliente { get; set; } // si está asociado a un cliente
        public bool EstaAnulado { get; set; }
        // 📝 Otros datos
        [StringLength(500)]
        public string? Nota { get; set; } = string.Empty;
    }
}
