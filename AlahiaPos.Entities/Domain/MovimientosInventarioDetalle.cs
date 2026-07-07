using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlahiaPos.Entities.Domain
{
    public class MovimientosInventarioDetalle
    {
        [Key]
        public int Id { get; set; }

        // =========================================
        // 🔥 MOVIMIENTO
        // =========================================

        public int IdMovimientoInventario { get; set; }

        [ForeignKey(nameof(IdMovimientoInventario))]
        public virtual MovimientosInventario?
            MovimientoInventario
        { get; set; }

        // =========================================
        // 🔥 PRODUCTO
        // =========================================

        public int IdProducto { get; set; }

        [ForeignKey(nameof(IdProducto))]
        public virtual Productos?
            Producto
        { get; set; }

        // =========================================
        // 🔥 CANTIDAD
        // =========================================

        public decimal Cantidad { get; set; }

        // =========================================
        // 🔥 STOCK ANTERIOR
        // =========================================

        public decimal StockAnterior { get; set; }

        // =========================================
        // 🔥 STOCK NUEVO
        // =========================================

        public decimal StockNuevo { get; set; }

        // =========================================
        // 🔥 PRECIO
        // =========================================

        public decimal? Precio { get; set; }

        // =========================================
        // 🔥 SUBTOTAL
        // =========================================

        public decimal? SubTotal { get; set; }

        // =========================================
        // 🔥 OBSERVACION
        // =========================================

        public string? Observacion { get; set; }

        // =========================================
        // 🔥 FECHA
        // =========================================

        public DateTime Fecha { get; set; }
            = DateTime.Now;
    }
}
