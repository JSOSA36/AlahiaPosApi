using System.Collections.Generic;

namespace AlahiaPos.Entities.Domain
{
    public static class GastoComprobanteTipos
    {
        public const string SinComprobante = "Sin comprobante";
        public const string GastosMenores = "Comprobante para Gastos Menores";
        public const string Recibo = "Recibo";
        public const string Ticket = "Ticket";
        public const string Otro = "Otro";

        /// <summary>Tipo e-CF DGII para Gastos Menores (secuencia electrónica E43).</summary>
        public const int TipoEcfGastosMenores = 43;

        public static readonly IReadOnlyList<string> Todos = new[]
        {
            SinComprobante,
            GastosMenores,
        };

        public static bool EsGastosMenores(string? tipo) =>
            string.Equals(tipo?.Trim(), GastosMenores, System.StringComparison.OrdinalIgnoreCase);
    }

    public static class CategoriaGastoDefaults
    {
        public static readonly IReadOnlyList<(string Nombre, int Orden)> Iniciales = new[]
        {
            ("Transporte", 1),
            ("Combustible", 2),
            ("Publicidad", 3),
            ("Electricidad", 4),
            ("Agua", 5),
            ("Internet", 6),
            ("Material de oficina", 7),
            ("Limpieza", 8),
            ("Reparaciones", 9),
            ("Honorarios", 10),
            ("Impuestos", 11),
            ("Caja chica", 12),
            ("Otros", 99),
        };
    }
}
