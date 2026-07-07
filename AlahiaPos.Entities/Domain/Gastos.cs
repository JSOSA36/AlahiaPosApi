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

        public int IdProveedor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public string? Orien { get; set; }

        public string? Detalle { get; set; }

        public int? IdEmpleado { get; set; }

        public int? IdUsuario { get; set; }

        public bool? EstaCerrada { get; set; }

        public bool EstaAnulado { get; set; }

        // 🔥 Nuevos campos
        [StringLength(100)]
        public string FormaPago { get; set; } = "EFECTIVO";
        public int? IdCajaCierre { get; set; }
        public int? IdCuentaFinanciera { get; set; }

        [StringLength(100)]
        public string? Referencia { get; set; }

        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuarios? Usuario { get; set; }

        [ForeignKey(nameof(IdCuentaFinanciera))]
        public virtual CuentaFinanciera? CuentaFinanciera { get; set; }

        [NotMapped]
        public Proveedores? Proveedores { get; set; }
    }
}