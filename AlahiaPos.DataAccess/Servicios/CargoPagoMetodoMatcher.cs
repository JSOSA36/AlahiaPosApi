using System;
using System.Collections.Generic;
using System.Linq;
using AlahiaPos.Entities.Domain;

namespace AlahiaPos.DataAccess.Servicios
{
    public static class CargoPagoMetodoMatcher
    {
        public static bool CoincideRegla(CargoPagoRegla regla, string? metodoPago)
        {
            if (regla == null) return false;
            return CoincideRegla(regla.GrupoMetodo, regla.MetodosVinculados, metodoPago);
        }

        public static bool CoincideRegla(string? grupoMetodo, IEnumerable<string>? metodosVinculados, string? metodoPago)
        {
            if (CoincideVinculado(metodosVinculados, metodoPago))
                return true;

            var grupo = (grupoMetodo ?? "").Trim().ToUpperInvariant();
            if (grupo == CargoPagoGrupos.Personalizado)
                return false;

            return CoincideGrupo(grupoMetodo, metodoPago);
        }

        public static bool CoincideRegla(string? grupoMetodo, string? metodosVinculados, string? metodoPago)
            => CoincideRegla(grupoMetodo, SplitVinculados(metodosVinculados), metodoPago);

        public static bool CoincideGrupo(string? grupoMetodo, string? metodoPago)
        {
            var grupo = (grupoMetodo ?? "").Trim().ToUpperInvariant();
            if (grupo.Length == 0 || grupo == CargoPagoGrupos.Personalizado)
                return false;
            if (grupo == "TODOS")
                return TieneMetodoReal(metodoPago);

            var metodo = Normalizar(metodoPago);
            if (metodo.Length == 0) return false;

            return grupo switch
            {
                "TARJETA" => EsTarjeta(metodo),
                "EFECTIVO" => metodo.Contains("EFECTIVO") || metodo.Contains("CASH"),
                "TRANSFERENCIA" => EsTransferencia(metodo),
                "CHEQUE" => metodo.Contains("CHEQUE") || metodo.Contains("CHECK"),
                _ => string.Equals(grupo, metodo, StringComparison.OrdinalIgnoreCase)
            };
        }

        /// <summary>Compatibilidad: solo grupo, sin vínculos de empresa.</summary>
        public static bool Coincide(string grupoMetodo, string? metodoPago)
            => CoincideGrupo(grupoMetodo, metodoPago);

        public static bool CoincideVinculado(IEnumerable<string>? metodosVinculados, string? metodoPago)
        {
            var metodo = Normalizar(metodoPago);
            if (metodo.Length == 0) return false;

            foreach (var vinculo in metodosVinculados ?? Enumerable.Empty<string>())
            {
                if (metodo == Normalizar(vinculo))
                    return true;
            }

            return false;
        }

        public static bool CoincideVinculado(string? metodosVinculados, string? metodoPago)
            => CoincideVinculado(SplitVinculados(metodosVinculados), metodoPago);

        public static IReadOnlyList<string> ParseVinculados(string? raw)
            => SplitVinculados(raw)
                .Select(Normalizar)
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>Conserva el nombre tal como está en MetodoPagoCuenta.</summary>
        public static IReadOnlyList<string> SplitVinculados(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            return raw
                .Split(new[] { '|', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        public static string SerializarVinculados(IEnumerable<string>? metodos)
        {
            if (metodos == null)
                return "";

            return string.Join("|", metodos
                .Select(m => (m ?? "").Trim())
                .Where(m => m.Length > 0)
                .GroupBy(m => m, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()));
        }

        public static bool EsTarjeta(string metodoNormalizado)
        {
            if (EsTransferencia(metodoNormalizado)) return false;
            return metodoNormalizado.Contains("TARJETA")
                || metodoNormalizado.Contains("VISA")
                || metodoNormalizado.Contains("MASTER")
                || metodoNormalizado.Contains("CARD")
                || metodoNormalizado.Contains("BILLET")
                || metodoNormalizado.Contains("AZUL")
                || metodoNormalizado.Contains("CARDNET")
                || metodoNormalizado.Contains("TDC");
        }

        public static bool EsTransferencia(string metodoNormalizado)
            => metodoNormalizado.Contains("TRANSFER")
               || metodoNormalizado.Contains("ACH")
               || metodoNormalizado.Contains("DEPOSITO")
               || metodoNormalizado.Contains("DEPÓSITO");

        private static bool TieneMetodoReal(string? metodoPago)
        {
            var metodo = Normalizar(metodoPago);
            return metodo.Length > 0 && metodo != "NOTACREDITO";
        }

        public static string Normalizar(string? metodo)
            => (metodo ?? "").Trim().ToUpperInvariant();
    }
}
