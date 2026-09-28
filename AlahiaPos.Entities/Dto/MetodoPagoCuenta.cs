using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Dto
{
    public class MetodoPagoCuenta
    {
        [Key]
        public int IdMetodoPagoCuenta { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [MaxLength(100)]
        public string MetodoPago { get; set; }
            = string.Empty;
        /*
            EFECTIVO
            TARJETA
            POPULAR
            BHD
            RESERVAS
        */

        public int IdCuentaFinanciera { get; set; }

        public bool Activo { get; set; }
            = true;

        /// <summary>
        /// Si es true, este método se ofrece al cotejar cobros de ARS
        /// y el dinero entra a IdCuentaFinanciera.
        /// </summary>
        public bool EsCobroArs { get; set; }

        /* =========================================
        🔥 RELACION
        ========================================= */

        [ForeignKey(nameof(IdCuentaFinanciera))]
        public CuentaFinanciera? CuentaFinanciera { get; set; }
    }
}
