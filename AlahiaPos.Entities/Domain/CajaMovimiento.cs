using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class CajaMovimiento
    {

        [Key]
        public int IdCajaMovimiento { get; set; }

        public int IdCajaApertura { get; set; }

        public int IdEmpresa { get; set; }

        public int IdUsuario { get; set; }

        public DateTime FechaMovimiento { get; set; }
            = DateTime.Now;

        /*
          ENTRADA
          SALIDA
        */
        public string TipoMovimiento { get; set; }
            = string.Empty;

        /*
          GASTO
          AJUSTE
          COMPRA
          OTRO
        */
        public string? Categoria { get; set; }

        public string? Descripcion { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        /* =====================================
        🔥 RELACIÓN
        ===================================== */

        [ForeignKey(nameof(IdCajaApertura))]
        public CajaApertura? CajaApertura { get; set; }
    }
}
