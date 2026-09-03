using System;
using System.Collections.Generic;
using System.Linq;

namespace AlahiaPos.DataAccess.Servicios
{
    public static class CargoPagoMetodoMatcher
    {
        public static bool Coincide(string grupoMetodo, string? metodoPago)
        {
            var grupo = (grupoMetodo ?? "").Trim().ToUpperInvariant();
            if (grupo.Length == 0) return false;
            if (grupo == "TODOS") return TieneMetodoReal(metodoPago);

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

        public static bool EsTarjeta(string metodoNormalizado)
        {
            if (EsTransferencia(metodoNormalizado)) return false;
            return metodoNormalizado.Contains("TARJETA")
                || metodoNormalizado.Contains("VISA")
                || metodoNormalizado.Contains("MASTER")
                || metodoNormalizado.Contains("CARD");
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
