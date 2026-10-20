namespace AlahiaPos.Entities.Delivery
{
    public static class DeliveryTarifaCalculador
    {
        public static decimal Calcular(
            decimal tarifaBase,
            decimal kilometros,
            decimal precioPorKm,
            decimal minimo,
            decimal redondeo)
        {
            if (kilometros < 0) kilometros = 0;
            if (tarifaBase < 0) tarifaBase = 0;
            if (precioPorKm < 0) precioPorKm = 0;
            if (minimo < 0) minimo = 0;

            var recorrido = decimal.Round(kilometros * precioPorKm, 2, MidpointRounding.AwayFromZero);
            var subtotal = decimal.Round(tarifaBase + recorrido, 2, MidpointRounding.AwayFromZero);
            if (subtotal < minimo)
                subtotal = minimo;
            if (redondeo > 0)
                subtotal = RedondearArriba(subtotal, redondeo);
            return subtotal;
        }

        public static decimal MontoATransferir(decimal totalOrden, decimal costoDelivery, bool cobrarAlEntregar)
        {
            if (!cobrarAlEntregar)
                return totalOrden;
            var monto = totalOrden - costoDelivery;
            return monto < 0 ? 0 : monto;
        }

        public static bool CobrarAlEntregar(bool esDelivery, bool esTransferencia, decimal costoDelivery, bool clientePidePagarAparte)
            => esDelivery && esTransferencia && costoDelivery > 0 && clientePidePagarAparte;

        public static decimal RedondearArriba(decimal valor, decimal multiplo)
        {
            if (multiplo <= 0)
                return valor;
            var pasos = valor / multiplo;
            return decimal.Ceiling(pasos) * multiplo;
        }
    }
}
