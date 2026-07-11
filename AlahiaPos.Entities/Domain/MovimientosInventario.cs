using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class MovimientosInventario
    {
        [Key]
        public int Id { get; set; }

        // =========================================
        // 🔥 ENTRADA / SALIDA
        // =========================================

        public string TipoMovimiento { get; set; } = string.Empty;

        // =========================================
        // 🔥 COMPRA / AJUSTE / PERDIDA
        // =========================================

        public string Motivo { get; set; } = string.Empty;

        // =========================================
        // 🔥 REFERENCIA
        // =========================================
        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuarios?
        Usuario
        {
            get;
            set;
        }
        public string? Referencia { get; set; }

        // =========================================
        // 🔥 OBSERVACION
        // =========================================

        public string? Observacion { get; set; }

        // =========================================
        // 🔥 FECHA
        // =========================================

        public DateTime Fecha { get; set; }
            = DateTime.Now;

        // =========================================
        // 🔥 USUARIO
        // =========================================

        public int? IdUsuario { get; set; }

        // =========================================
        // 🔥 EMPRESA
        // =========================================

        public int IdEmpresa { get; set; }

        public int? IdAlmacen { get; set; }

        public int? IdAlmacenDestino { get; set; }

        // =========================================
        // 🔥 ACTIVO
        // =========================================

        public bool Activo { get; set; } = true;

        // =========================================
        // 🔥 DETALLE
        // =========================================

        public virtual ICollection<MovimientosInventarioDetalle>
            Detalles
        { get; set; }
            = new List<MovimientosInventarioDetalle>();

    }
}
