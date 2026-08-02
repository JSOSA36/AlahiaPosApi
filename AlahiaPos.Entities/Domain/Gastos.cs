using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AlahiaPos.Entities.Domain
{
    public class Gastos : BaseEntity
    {
        [Key]
        public int IdGasto { get; set; }

        public string? TipoGasto { get; set; }

        /// <summary>FK a catálogo CategoriasGasto (clasificación ERP).</summary>
        public int? IdCategoriaGasto { get; set; }

        /// <summary>Sin comprobante | Comprobante para Gastos Menores | Recibo | Ticket | Otro</summary>
        [StringLength(80)]
        public string? TipoComprobante { get; set; }

        [StringLength(50)]
        public string? NumeroComprobante { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaComprobante { get; set; }

        [StringLength(20)]
        public string? RncEmisorComprobante { get; set; }

        [StringLength(150)]
        public string? NombreEmisorComprobante { get; set; }

        public int IdProveedor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public string? Orien { get; set; }

        public string? Detalle { get; set; }

        public int? IdEmpleado { get; set; }

        public int? IdUsuario { get; set; }

        public bool? EstaCerrada { get; set; }

        public bool EstaAnulado { get; set; }

        [StringLength(500)]
        public string? MotivoAnulacion { get; set; }

        // 🔥 Nuevos campos
        [StringLength(100)]
        public string FormaPago { get; set; } = "EFECTIVO";
        public int? IdCajaCierre { get; set; }
        public int? IdCuentaFinanciera { get; set; }

        [StringLength(100)]
        public string? Referencia { get; set; }

        /// <summary>COMPRAS | MANUAL | CONCILIACION — origen del registro de gasto.</summary>
        [StringLength(30)]
        public string? OrigenModulo { get; set; }

        public int? IdOrdenCompraDetalle { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuarios? Usuario { get; set; }

        [ForeignKey(nameof(IdCuentaFinanciera))]
        public virtual CuentaFinanciera? CuentaFinanciera { get; set; }

        [ForeignKey(nameof(IdCategoriaGasto))]
        public virtual CategoriaGasto? CategoriaGasto { get; set; }

        [NotMapped]
        public Proveedores? Proveedores { get; set; }
    }
}