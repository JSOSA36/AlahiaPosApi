using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class CuentaFinanciera
    {
        [Key]
        public int IdCuentaFinanciera { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal SaldoDisponible { get; set; }
        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }
            = string.Empty;
        /*
            Caja Chica
            Popular Principal
            BHD Operaciones
        */

        [Required]
        [MaxLength(50)]
        public string TipoCuenta { get; set; }
            = string.Empty;
        /*
            CAJA
            BANCO
            TARJETA
        */

        [MaxLength(100)]
        public string? Banco { get; set; }

        [MaxLength(100)]
        public string? NumeroCuenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceInicial { get; set; }

        [Column(TypeName = "date")]
        public DateTime? FechaSaldoInicial { get; set; }

        public bool PermiteMovimientosManuales { get; set; } = true;

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Icono { get; set; }

        public bool Activa { get; set; }
            = true;

        public DateTime FechaCreacion { get; set; }
            = DateTime.Now;

        [MaxLength(30)]
        public string? Codigo { get; set; }

        public int? IdTesoreriaSubtipoCuenta { get; set; }

        [MaxLength(3)]
        public string Moneda { get; set; } = "DOP";

        public int? IdCuentaContable { get; set; }

        public bool EsPrincipal { get; set; }

        public bool PermiteSaldoNegativo { get; set; }

        [MaxLength(500)]
        public string? Descripcion { get; set; }

        public DateTime? FechaUltimaConciliacion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UltimoSaldoConciliado { get; set; }

        [ForeignKey(nameof(IdTesoreriaSubtipoCuenta))]
        public TesoreriaSubtipoCuenta? TesoreriaSubtipoCuenta { get; set; }

        [ForeignKey(nameof(IdCuentaContable))]
        public CuentaContable? CuentaContable { get; set; }
    }
}
