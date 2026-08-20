using System;

namespace AlahiaPos.Entities.Domain
{
    public static class NivelesSoporte
    {
        public const string Standard = "STANDARD";
        public const string Gold = "GOLD";
        public const string Premium = "PREMIUM";

        public static string Normalizar(string? valor)
        {
            var v = (valor ?? "").Trim().ToUpperInvariant().Replace(' ', '_');
            if (v == Gold || v == "ORO")
                return Gold;
            if (v == Premium
                || v == "PREMIUM_PLUS"
                || v == "PREMIUMPLUS"
                || v == "PLATINUM"
                || v == "PLATINO")
                return Premium;
            return Standard;
        }
    }
}
