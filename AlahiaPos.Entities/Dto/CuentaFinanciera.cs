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

        [MaxLength(30)]
        public string? Color { get; set; }

        [MaxLength(50)]
        public string? Icono { get; set; }

        public bool Activa { get; set; }
            = true;

        public DateTime FechaCreacion { get; set; }
            = DateTime.Now;
    }
}
