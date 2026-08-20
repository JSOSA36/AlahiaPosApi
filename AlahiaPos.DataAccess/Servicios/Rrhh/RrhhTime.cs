using AlahiaPos.Entities.Dto;
using System.Globalization;

namespace AlahiaPos.DataAccess.Servicios.Rrhh
{
    internal static class RrhhTime
    {
        public static TimeSpan? Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var v = value.Trim();
            if (TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out var ts))
                return ts;
            if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.TimeOfDay;
            return null;
        }

        public static TimeSpan ParseRequired(string? value, string fallback)
            => Parse(value) ?? Parse(fallback) ?? TimeSpan.FromHours(8);

        public static string? Format(TimeSpan? value) =>
            value.HasValue ? value.Value.ToString(@"hh\:mm") : null;

        public static DateTime DateOnly(DateTime d) => d.Date;

        public static byte DiaSemanaLunes(DateTime fecha)
        {
            // 1 = lunes … 7 = domingo (igual que HorariosEstilista)
            var dow = (int)fecha.DayOfWeek;
            return (byte)(dow == 0 ? 7 : dow);
        }

        public static int MinutosEntre(TimeSpan inicio, TimeSpan fin)
        {
            var m = (int)(fin - inicio).TotalMinutes;
            if (m < 0) m += 24 * 60;
            return m;
        }
    }
}
