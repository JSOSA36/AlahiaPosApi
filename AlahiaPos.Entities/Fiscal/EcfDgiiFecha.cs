using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AlahiaPos.Entities.Fiscal
{
    /// <summary>
    /// Fechas e-CF DGII: dd-MM-yyyy [HH:mm:ss]. InvariantCulture.TryParse trata 27-08 como mes 27 y falla.
    /// </summary>
    public static class EcfDgiiFecha
    {
        private static readonly string[] Formatos =
        {
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy H:mm:ss",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd"
        };

        public static DateTime? Parse(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            var s = valor.Trim();
            if (DateTime.TryParseExact(s, Formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt;
            if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out dt))
                return dt;
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt;
            return null;
        }

        public static DateTime? ExtraerFechaHoraFirmaXml(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            var m = Regex.Match(
                xml,
                @"<FechaHoraFirma>\s*([^<]+)\s*</FechaHoraFirma>",
                RegexOptions.IgnoreCase);
            return m.Success ? Parse(m.Groups[1].Value) : null;
        }
    }
}
