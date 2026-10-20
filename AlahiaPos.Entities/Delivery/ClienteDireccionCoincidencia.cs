namespace AlahiaPos.Entities.Delivery
{
    public static class ClienteDireccionCoincidencia
    {
        public static string? CopiarTexto(string? valor)
            => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

        public static bool MismaUbicacion(
            string? direccionA,
            decimal? latA,
            decimal? lngA,
            string? direccionB,
            decimal? latB,
            decimal? lngB)
        {
            if (latA.HasValue && lngA.HasValue && latB.HasValue && lngB.HasValue)
            {
                return DeliveryCotizacionMotor.ClaveCoord(latA.Value) == DeliveryCotizacionMotor.ClaveCoord(latB.Value)
                    && DeliveryCotizacionMotor.ClaveCoord(lngA.Value) == DeliveryCotizacionMotor.ClaveCoord(lngB.Value);
            }

            var a = Normalizar(direccionA);
            var b = Normalizar(direccionB);
            return a.Length > 0 && a == b;
        }

        private static string Normalizar(string? valor)
            => (valor ?? "").Trim().ToLowerInvariant();
    }
}
