using System.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>
    /// Reglas compartidas de RNC/Cédula para Validar (Motor de Definiciones).
    /// Alineado con SanearRnc del POS: 9 u 11 dígitos, no todo ceros.
    /// </summary>
    public static class EcfRncRules
    {
        public static string? SoloDigitos(string? rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc)) return null;
            var digits = new string(rnc.Where(char.IsDigit).ToArray());
            return string.IsNullOrEmpty(digits) ? null : digits;
        }

        public static bool EsRncValido(string? rnc)
        {
            var digits = SoloDigitos(rnc);
            if (digits is null) return false;
            if (digits.Length is not (9 or 11)) return false;
            if (digits.All(c => c == '0')) return false;
            return true;
        }

        public static void ExigirRncValido(string? rnc, string codigoTipo, string campo)
        {
            if (!EsRncValido(rnc))
                throw new InvalidOperationException(
                    $"{codigoTipo}: {campo} es obligatorio y debe tener 9 u 11 dígitos válidos (no ceros).");
        }
    }
}
