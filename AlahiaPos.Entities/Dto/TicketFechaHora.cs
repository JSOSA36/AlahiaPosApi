using System;
using System.Globalization;

namespace AlahiaPos.Entities.Dto
{
    /// <summary>
    /// FechaEmision / FechaFirma de e-CF suelen venir solo con fecha (00:00).
    /// En el ticket se completa con la hora de la factura (campo Hora).
    /// No usar para armar el QR de DGII: esa marca debe coincidir con la firma.
    /// </summary>
    public static class TicketFechaHora
    {
        private static readonly string[] FormatosHora =
        {
            "hh:mm tt", "h:mm tt", "hh:mm:ss tt", "h:mm:ss tt",
            "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss"
        };

        public static DateTime? ParaImpresion(
            DateTime? fechaFirma,
            DateTime? fechaEmision,
            DateTime? fechaDocumento,
            string? hora)
        {
            var baseDt = fechaFirma ?? fechaEmision;
            if (!baseDt.HasValue)
                return Combinar(fechaDocumento, hora);

            if (baseDt.Value.TimeOfDay != TimeSpan.Zero)
                return baseDt.Value;

            return Combinar(baseDt, hora)
                ?? Combinar(fechaDocumento, hora)
                ?? baseDt.Value;
        }

        public static DateTime? Combinar(DateTime? fecha, string? hora)
        {
            if (!fecha.HasValue) return null;
            var time = ParseHora(hora);
            if (!time.HasValue) return null;
            return fecha.Value.Date.Add(time.Value);
        }

        private static TimeSpan? ParseHora(string? hora)
        {
            if (string.IsNullOrWhiteSpace(hora)) return null;
            var h = NormalizarAmPm(hora.Trim());

            foreach (var cultura in new[]
            {
                CultureInfo.InvariantCulture,
                CultureInfo.GetCultureInfo("en-US"),
                CultureInfo.GetCultureInfo("es-DO")
            })
            {
                if (DateTime.TryParseExact(h, FormatosHora, cultura, DateTimeStyles.None, out var dt))
                    return dt.TimeOfDay;
                if (DateTime.TryParse(h, cultura, DateTimeStyles.None, out dt))
                    return dt.TimeOfDay;
            }

            return null;
        }

        private static string NormalizarAmPm(string hora)
        {
            return hora
                .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase)
                .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
                .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase);
        }
    }
}
