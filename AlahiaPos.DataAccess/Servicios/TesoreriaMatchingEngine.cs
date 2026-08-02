using System;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Motor de scoring 1:1 para conciliación bancaria (explicable y testeable).
    /// Auto-match solo con candidato único de confianza alta; ambigüedad bloquea auto-conciliación.
    /// </summary>
    public static class TesoreriaMatchingEngine
    {
        public const decimal ScoreMinimoCandidato = 0.55m;
        public const decimal ScoreAutoMatch = 0.95m;
        public const decimal ScoreSegundoMaxParaAuto = 0.85m;
        public const decimal MargenAmbiguedad = 0.10m;

        public static decimal CalcularScore(TesoreriaExtractoLinea linea, MovimientoFinanciero mov)
        {
            decimal score = 0.55m; // monto + dirección ya filtrados

            if (mov.FechaMovimiento.Date == linea.FechaMovimiento.Date)
                score += 0.25m;
            else if (Math.Abs((mov.FechaMovimiento.Date - linea.FechaMovimiento.Date).TotalDays) <= 1)
                score += 0.15m;
            else
                score += 0.05m;

            if (!string.IsNullOrWhiteSpace(linea.Referencia))
            {
                var r = linea.Referencia.Trim();
                if (string.Equals(mov.NumeroComprobante, r, StringComparison.OrdinalIgnoreCase)
                    || (mov.Motivo?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (mov.Observacion?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (mov.ClaveIdempotencia?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false))
                    score += 0.20m;
            }

            if (!string.IsNullOrWhiteSpace(linea.Descripcion) && !string.IsNullOrWhiteSpace(mov.Motivo))
            {
                var d = linea.Descripcion.Trim();
                if (mov.Motivo.Contains(d, StringComparison.OrdinalIgnoreCase)
                    || d.Contains(mov.Motivo, StringComparison.OrdinalIgnoreCase))
                    score += 0.10m;
            }

            return Math.Min(1.0m, score);
        }

        /// <summary>
        /// Decide el resultado del matching 1:1 a partir de scores ya ordenados desc.
        /// </summary>
        public static MatchDecision Decidir(
            decimal mejorScore,
            decimal? segundoScore,
            int candidatos,
            bool evidenciaIdentificadora = false)
        {
            if (candidatos <= 0 || mejorScore < ScoreMinimoCandidato)
                return MatchDecision.SinMatch;

            if (candidatos >= 2
                && segundoScore.HasValue
                && mejorScore - segundoScore.Value < MargenAmbiguedad)
                return MatchDecision.Ambiguo;

            var unicoAlto = mejorScore >= ScoreAutoMatch
                && (candidatos == 1 || (segundoScore ?? 0) < ScoreSegundoMaxParaAuto);

            // Sin referencia/texto suficiente, nunca auto-conciliar (solo sugerir).
            if (unicoAlto && evidenciaIdentificadora)
                return MatchDecision.AutoConciliar;

            return MatchDecision.Sugerir;
        }

        /// <summary>
        /// Auto-match requiere evidencia de referencia o texto además de monto/fecha,
        /// para reducir falsos positivos en montos repetidos el mismo día.
        /// </summary>
        public static bool TieneEvidenciaIdentificadora(TesoreriaExtractoLinea linea, MovimientoFinanciero mov)
        {
            if (!string.IsNullOrWhiteSpace(linea.Referencia))
            {
                var r = linea.Referencia.Trim();
                if (r.Length >= 3
                    && (string.Equals(mov.NumeroComprobante, r, StringComparison.OrdinalIgnoreCase)
                        || (mov.Motivo?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (mov.Observacion?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false)
                        || (mov.ClaveIdempotencia?.Contains(r, StringComparison.OrdinalIgnoreCase) ?? false)))
                    return true;
            }

            if (!string.IsNullOrWhiteSpace(linea.Descripcion) && !string.IsNullOrWhiteSpace(mov.Motivo))
            {
                var d = linea.Descripcion.Trim();
                if (d.Length >= 6
                    && (mov.Motivo.Contains(d, StringComparison.OrdinalIgnoreCase)
                        || d.Contains(mov.Motivo, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        public static bool MismaDireccion(TesoreriaExtractoLinea linea, MovimientoFinanciero m, int idCuenta)
        {
            var esDebitoBanco = linea.Debito > 0;

            if (esDebitoBanco)
            {
                if (string.Equals(m.TipoMovimiento, "SALIDA", StringComparison.OrdinalIgnoreCase)
                    && m.IdCuentaOrigen == idCuenta)
                    return true;

                if (string.Equals(m.TipoMovimiento, "TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)
                    && m.IdCuentaOrigen == idCuenta)
                    return true;

                return false;
            }

            if (string.Equals(m.TipoMovimiento, "ENTRADA", StringComparison.OrdinalIgnoreCase)
                && m.IdCuentaDestino == idCuenta)
                return true;

            if (string.Equals(m.TipoMovimiento, "TRANSFERENCIA", StringComparison.OrdinalIgnoreCase)
                && m.IdCuentaDestino == idCuenta)
                return true;

            return false;
        }

        /// <summary>
        /// Dirección abstracta sin exigir que el movimiento esté en la cuenta del extracto.
        /// Crédito bancario ↔ ENTRADA (cualquier cuenta); Débito bancario ↔ SALIDA.
        /// </summary>
        public static bool MismaDireccionSinCuenta(TesoreriaExtractoLinea linea, MovimientoFinanciero m)
        {
            var esDebitoBanco = linea.Debito > 0;
            var tipo = (m.TipoMovimiento ?? string.Empty).Trim().ToUpperInvariant();

            if (esDebitoBanco)
                return tipo is "SALIDA" or "TRANSFERENCIA";

            return tipo is "ENTRADA" or "TRANSFERENCIA";
        }

        /// <summary>
        /// Matching cruzado nunca auto-concilia: solo Sugerir o Ambiguo.
        /// </summary>
        public static MatchDecision DecidirCruzado(
            decimal mejorScore,
            decimal? segundoScore,
            int candidatos)
        {
            if (candidatos <= 0 || mejorScore < ScoreMinimoCandidato)
                return MatchDecision.SinMatch;

            if (candidatos >= 2
                && segundoScore.HasValue
                && mejorScore - segundoScore.Value < MargenAmbiguedad)
                return MatchDecision.Ambiguo;

            return MatchDecision.Sugerir;
        }

        public static string NivelConfianzaCruzado(decimal score, bool evidenciaIdentificadora, int candidatos)
        {
            if (candidatos != 1)
                return "BAJA";

            if (score >= ScoreAutoMatch && evidenciaIdentificadora)
                return "ALTA";

            if (score >= 0.80m)
                return "MEDIA";

            return "BAJA";
        }
    }

    public enum MatchDecision
    {
        SinMatch,
        AutoConciliar,
        Sugerir,
        Ambiguo
    }
}
