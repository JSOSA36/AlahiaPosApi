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
    }
}
