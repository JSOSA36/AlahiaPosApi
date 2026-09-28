using System;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Cruce por turno de caja: stock al abrir − vendido (+ otros movs) vs stock al cerrar.
    /// </summary>
    public class CruceStockVentaLineaDto
    {
        public int IdCajaCierre { get; set; }

        public DateTime FechaApertura { get; set; }

        public DateTime FechaCierre { get; set; }

        public int IdUsuario { get; set; }

        public string Usuario { get; set; } = string.Empty;

        public int IdProducto { get; set; }

        public string Producto { get; set; } = string.Empty;

        public decimal StockAlAbrir { get; set; }

        public decimal CantidadVendida { get; set; }

        /// <summary>
        /// Neto de movimientos que no son VENTA en el turno (entradas +, otras salidas −).
        /// </summary>
        public decimal OtrosMovimientos { get; set; }

        /// <summary>
        /// StockAlAbrir − CantidadVendida + OtrosMovimientos.
        /// </summary>
        public decimal StockEsperado { get; set; }

        public decimal StockAlCerrar { get; set; }

        /// <summary>
        /// StockAlCerrar − StockEsperado. Cero = cuadra.
        /// </summary>
        public decimal Diferencia { get; set; }

        public bool Cuadra { get; set; }
    }
}
