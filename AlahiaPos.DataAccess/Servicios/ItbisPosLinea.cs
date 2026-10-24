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
        /// Si la empresa factura con ITBIS, el comprobante no puede ir en cero.
        /// Empresas sin ITBIS (ej. Dra Sena) no se tocan.
        /// </summary>
        public static decimal AsegurarEnComprobante(
            decimal itbis,
            decimal precioUnitario,
            decimal cantidad,
            bool esComprobante,
            bool empresaFacturaConItbis,
            decimal? tasaProducto = null)
        {
            var itbisSafe = itbis < 0m ? 0m : itbis;
            var extendido = ExtenderSiEsUnitario(
                itbisSafe,
                (precioUnitario * cantidad) + itbisSafe,
                cantidad);

            if (!esComprobante || !empresaFacturaConItbis || extendido > 0.009m)
                return extendido;

            var tasa = ResolverTasa(tasaProducto);
            return Math.Round(precioUnitario * cantidad * tasa, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Factura de consumo (E32): el ITBIS es obligatorio si la empresa factura con ITBIS
        /// y el producto está gravado. El precio que llegó en cero es el monto cobrado;
        /// el impuesto se desglosa adentro y el total de la línea no cambia.
        /// El interruptor del POS sigue pudiendo dejar en cero una factura sin ese comprobante.
        /// </summary>
        public static ItbisLineaResuelta ResolverLinea(
            decimal itbisEnviado,
            decimal precioUnitario,
            decimal cantidad,
            bool esComprobante,
            bool esConsumo,
            bool empresaFacturaConItbis,
            bool productoGrava,
            bool respetarItbisEnCero,
            decimal? tasaProducto = null)
        {
            var cant = cantidad <= 0m ? 1m : cantidad;
            var itbisSafe = itbisEnviado < 0m ? 0m : itbisEnviado;

            // El precio en cero con el interruptor del POS es el monto cobrado (bruto).
            // En consumo se abre el ITBIS adentro. En otro comprobante se respeta el cero.
            if (esConsumo
                && respetarItbisEnCero
                && empresaFacturaConItbis
                && productoGrava
                && itbisSafe <= 0.009m)
            {
                return DesglosarItbisIncluido(precioUnitario, cant, tasaProducto);
            }

            var itbis = AsegurarEnComprobante(
                itbisSafe,
                precioUnitario,
                cant,
                esComprobante,
                empresaFacturaConItbis && !respetarItbisEnCero,
                tasaProducto);

            return new ItbisLineaResuelta(precioUnitario, itbis, (precioUnitario * cant) + itbis);
        }

        public static ItbisLineaResuelta DesglosarItbisIncluido(
            decimal precioBrutoUnitario,
            decimal cantidad,
            decimal? tasaProducto = null)
        {
            var cant = cantidad <= 0m ? 1m : cantidad;
            var bruto = Math.Round(precioBrutoUnitario * cant, 2, MidpointRounding.AwayFromZero);
            var tasa = ResolverTasa(tasaProducto);
            var neto = Math.Round(bruto / (1m + tasa), 2, MidpointRounding.AwayFromZero);
            var itbis = Math.Round(neto * tasa, 2, MidpointRounding.AwayFromZero);
            if (neto + itbis != bruto)
                itbis = bruto - neto;

            var precioNeto = cant == 1m
                ? neto
                : Math.Round(neto / cant, 2, MidpointRounding.AwayFromZero);

            return new ItbisLineaResuelta(precioNeto, itbis, bruto);
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

    public readonly struct ItbisLineaResuelta
    {
        public ItbisLineaResuelta(decimal precioUnitario, decimal itbis, decimal subTotal)
        {
            PrecioUnitario = precioUnitario;
            Itbis = itbis;
            SubTotal = subTotal;
        }

        public decimal PrecioUnitario { get; }
        public decimal Itbis { get; }
        public decimal SubTotal { get; }
    }
}
