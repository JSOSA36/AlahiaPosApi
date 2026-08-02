using System;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Predicados canónicos de estado de línea de extracto.
    /// Una sola fuente de verdad para KPIs, saldos, cierre y filtros del workbench.
    /// </summary>
    public static class TesoreriaConciliacionEstados
    {
        public static bool EsPendienteBanco(string? estadoMatch) =>
            estadoMatch is "PENDIENTE" or "SUGERIDO" or "AMBIGUO" or "DUPLICADO" or "DIFERENCIA";

        public static bool EsConciliadaBanco(string? estadoMatch) =>
            estadoMatch is "CONFIRMADO" or "AUTO_CONCILIADO" or "NUEVO_MOV" or "RESUELTO";

        public static bool EsExcluidaBanco(string? estadoMatch) =>
            estadoMatch is "IGNORADO" or "DESCARTADO";

        /// <summary>Línea ya tratada (conciliada o excluida a propósito).</summary>
        public static bool EsResueltaBanco(string? estadoMatch) =>
            EsConciliadaBanco(estadoMatch) || EsExcluidaBanco(estadoMatch);

        public static bool EsSeleccionableMasivo(string? estadoMatch) =>
            estadoMatch is "PENDIENTE" or "SUGERIDO" or "AMBIGUO" or "DUPLICADO" or "DIFERENCIA";

        public static bool EqualsIgnoreCase(string? a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
