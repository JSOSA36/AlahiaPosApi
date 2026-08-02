using System;
using System.Security.Cryptography;
using System.Text;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Token firmado para links públicos de cotización POS (sin columna en BD).
    /// Formato: {idEmpresa}-{idFactura}-{firmaHex16}
    /// </summary>
    public static class CotizacionShareToken
    {
        // Clave de firma interna del producto (no es credencial de usuario).
        private static readonly byte[] Key =
            Encoding.UTF8.GetBytes("Alahia.POS.CotizacionShare.v1");

        public static string Create(int idEmpresa, int idFacturaHeader)
        {
            var sig = Sign(idEmpresa, idFacturaHeader);
            return $"{idEmpresa}-{idFacturaHeader}-{sig}";
        }

        public static bool TryParse(string? token, out int idEmpresa, out int idFacturaHeader)
        {
            idEmpresa = 0;
            idFacturaHeader = 0;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var parts = token.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out idEmpresa) || idEmpresa <= 0)
                return false;

            if (!int.TryParse(parts[1], out idFacturaHeader) || idFacturaHeader <= 0)
                return false;

            var expected = Sign(idEmpresa, idFacturaHeader);
            return FixedTimeEquals(expected, parts[2]);
        }

        private static string Sign(int idEmpresa, int idFacturaHeader)
        {
            var payload = $"{idEmpresa}.{idFacturaHeader}";
            using var hmac = new HMACSHA256(Key);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant()[..16];
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
                result |= a[i] ^ b[i];

            return result == 0;
        }
    }
}
