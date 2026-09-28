namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Tasas ISR del Formato 606 (campo 17). Honorarios, alquileres y otras rentas
    /// Art. 309 pasan de 10% a 15% el 1 jul 2026 (Ley 30-26). La tasa es la del pago.
    /// </summary>
    public static class RetencionIsrLegal
    {
        public static readonly DateTime VigenciaLey3026 = new(2026, 7, 1);

        public static decimal Tasa(int tipo606, DateTime fechaPago)
        {
            var postLey = fechaPago.Date >= VigenciaLey3026;
            return tipo606 switch
            {
                1 or 2 or 3 => postLey ? 0.15m : 0.10m,
                4 => 0.03m,
                5 => 0.01m,
                6 => 0.10m,
                7 => 0.05m,
                8 => 0.05m,
                9 => 0.01m,
                _ => 0m
            };
        }

        public static decimal Calcular(int tipo606, decimal baseImponible, DateTime fechaPago)
        {
            var tasa = Tasa(tipo606, fechaPago);
            if (tasa <= 0 || baseImponible <= 0)
                return 0m;
            return Math.Round(baseImponible * tasa, 2, MidpointRounding.AwayFromZero);
        }
    }
}
