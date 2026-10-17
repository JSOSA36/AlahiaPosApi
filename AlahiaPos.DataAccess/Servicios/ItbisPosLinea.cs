using System;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// El POS manda el ITBIS de una unidad. Al guardar, ese monto se sumaba una sola vez
    /// aunque la cantidad fuera mayor, y el ticket salía por debajo de la pantalla.
    /// </summary>
    public static class ItbisPosLinea
    {
        public static decimal ExtenderSiEsUnitario(decimal itbis, decimal subTotal, decimal cantidad)
        {
            if (cantidad <= 1m || itbis <= 0m)
                return itbis;

            var baseLinea = subTotal - itbis;
            if (baseLinea <= 0m)
                return itbis;

            var ratio = itbis / baseLinea;
            var ratioExtendido = ratio * cantidad;

            // 16% o 18% de la línea. El ITBIS de una sola unidad queda muy por debajo.
            if (ratio < 0.13m && ratioExtendido >= 0.13m && ratioExtendido <= 0.22m)
                return Math.Round(itbis * cantidad, 2, MidpointRounding.AwayFromZero);

            return itbis;
        }

        /// <summary>
        /// Consumo, crédito fiscal y gubernamental siempre gravan. Si el cajero apagó el ITBIS, se calcula.
        /// </summary>
        public static decimal AsegurarEnComprobante(
            decimal itbis,
            decimal precioUnitario,
            decimal cantidad,
            bool esComprobante,
            decimal? tasaProducto = null)
        {
            var itbisSafe = itbis < 0m ? 0m : itbis;
            var extendido = ExtenderSiEsUnitario(
                itbisSafe,
                (precioUnitario * cantidad) + itbisSafe,
                cantidad);

            if (!esComprobante || extendido > 0.009m)
                return extendido;

            var tasa = ResolverTasa(tasaProducto);
            return Math.Round(precioUnitario * cantidad * tasa, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolverTasa(decimal? tasaProducto)
        {
            if (tasaProducto == null)
                return 0.18m;
            if (tasaProducto.Value > 0m && tasaProducto.Value < 1m)
                return tasaProducto.Value;
            if (tasaProducto.Value >= 1m && tasaProducto.Value <= 100m)
                return tasaProducto.Value / 100m;
            return 0.18m;
        }
    }
}
