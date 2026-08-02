namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// Cálculo de saldos contables según naturaleza de la cuenta.
    /// Origen único: movimientos del Libro Diario (Débito/Crédito).
    /// </summary>
    public static class ContabilidadSaldoHelper
    {
        public const string ResultadoEjercicioCodigo = "RE";
        public const string ResultadoEjercicioNombre = "Resultado del Ejercicio";

        /// <summary>Activo, Gastos y Costos: naturaleza deudora.</summary>
        public static bool EsNaturalezaDeudora(string? tipoCuenta)
        {
            return tipoCuenta is ContabilidadConstantes.TipoActivo
                or ContabilidadConstantes.TipoGastos
                or ContabilidadConstantes.TipoCostos;
        }

        /// <summary>Pasivo, Capital e Ingresos: naturaleza acreedora.</summary>
        public static bool EsNaturalezaAcreedora(string? tipoCuenta)
            => !EsNaturalezaDeudora(tipoCuenta);

        /// <summary>
        /// Saldo según naturaleza a partir de totales Débito/Crédito.
        /// Deudora: Débito − Crédito. Acreedora: Crédito − Débito.
        /// </summary>
        public static decimal SaldoPorNaturaleza(string? tipoCuenta, decimal debito, decimal credito)
        {
            return EsNaturalezaDeudora(tipoCuenta)
                ? debito - credito
                : credito - debito;
        }

        /// <summary>
        /// Convierte saldo raw (Débito − Crédito) a saldo por naturaleza.
        /// </summary>
        public static decimal DesdeSaldoRaw(string? tipoCuenta, decimal saldoRaw)
        {
            return EsNaturalezaDeudora(tipoCuenta) ? saldoRaw : -saldoRaw;
        }

        /// <summary>
        /// Utilidad (pérdida) neta del período a partir de movimientos P&amp;L.
        /// Ingresos (acreedora) − Costos (deudora) − Gastos (deudora).
        /// </summary>
        public static decimal CalcularUtilidadNeta(
            decimal ingresosNaturaleza,
            decimal costosNaturaleza,
            decimal gastosNaturaleza)
        {
            return ingresosNaturaleza - costosNaturaleza - gastosNaturaleza;
        }
    }
}
