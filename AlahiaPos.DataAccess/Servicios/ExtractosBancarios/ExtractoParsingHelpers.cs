using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Helpers de parseo compartidos por los adapters de extracto bancario.
    /// Extraídos de TesoreriaExtractoService preservando el comportamiento original.
    /// </summary>
    public static class ExtractoParsingHelpers
    {
        public static readonly CultureInfo CulturaDo = CultureInfo.GetCultureInfo("es-DO");

        public static bool TryParseFecha(string value, CultureInfo cultura, out DateTime fecha)
        {
            var formatos = new[]
            {
                "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy", "dd-MM-yyyy", "d/M/yyyy", "dd/MM/yy"
            };

            if (DateTime.TryParseExact(value.Trim(), formatos, cultura, DateTimeStyles.None, out fecha))
                return true;

            return DateTime.TryParse(value.Trim(), cultura, DateTimeStyles.None, out fecha);
        }

        public static decimal ParseDecimal(string value, CultureInfo cultura)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Trim()
                .Replace("RD$", "", StringComparison.OrdinalIgnoreCase)
                .Replace("$", "")
                .Replace(" ", "");

            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowLeadingSign, cultura, out var result))
                return result;

            // Normalizar 25.000,00 o 25,000.00
            var alt = value;
            if (value.Contains(',') && value.Contains('.'))
            {
                if (value.LastIndexOf(',') > value.LastIndexOf('.'))
                    alt = value.Replace(".", "").Replace(',', '.');
                else
                    alt = value.Replace(",", "");
            }
            else if (value.Contains(',') && !value.Contains('.'))
            {
                alt = value.Replace(',', '.');
            }

            if (decimal.TryParse(alt, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        public static Dictionary<string, int> MapHeaders(IList<string> headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var h = NormalizeHeader(headers[i]);
                if (string.IsNullOrWhiteSpace(h)) continue;

                if (h is "fecha" or "date" or "fechmov" or "fechamovimiento")
                    map["fecha"] = i;
                else if (h is "referencia" or "reference" or "ref" or "documento" or "nro" or "numero")
                    map["referencia"] = i;
                else if (h is "descripcion" or "descripción" or "concepto" or "detalle" or "description" or "narrativa")
                    map["descripcion"] = i;
                else if (h is "debito" or "débito" or "cargo" or "retiro" or "debit" or "dr")
                    map["debito"] = i;
                else if (h is "credito" or "crédito" or "abono" or "deposito" or "depósito" or "credit" or "cr")
                    map["credito"] = i;
                else if (h is "balance" or "saldo" or "saldoactual" or "runningbalance")
                    map["balance"] = i;
            }
            return map;
        }

        public static string NormalizeHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var s = value.Trim().ToLowerInvariant();
            s = s.Replace("(rd$)", "").Replace("rd$", "").Replace("$", "");
            s = Regex.Replace(s, @"\s+", "");
            return s
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
        }

        public static string GetCol(IList<string> cols, Dictionary<string, int> map, string key)
        {
            if (!map.TryGetValue(key, out var idx) || idx < 0 || idx >= cols.Count)
                return string.Empty;
            return cols[idx] ?? string.Empty;
        }

        public static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            foreach (var ch in line)
            {
                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if ((ch == ',' || ch == ';' || ch == '\t') && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(ch);
            }

            result.Add(current.ToString().Trim());
            return result;
        }

        public static string ExtractAfter(string line, string prefix)
        {
            var idx = line.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return line.Trim();
            return line[(idx + prefix.Length)..].Trim();
        }

        public static string StripBulletPrefix(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return line;
            return Regex.Replace(line.Trim(), @"^[•●▪◦\-\*]+\s*", string.Empty).Trim();
        }

        public static bool EsLineaMetaExtracto(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            if (Regex.IsMatch(line, @"^\d+$")) return true;
            if (line.Equals("•", StringComparison.Ordinal)) return true;
            if (line.StartsWith("Resumen", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Banco:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Empresa:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Cuenta:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Moneda:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Período:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Periodo:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Saldo Inicial:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Saldo Final:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Total Créditos:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Total Creditos:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Total Débitos:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Total Debitos:", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Débito", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("Debito", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("Fecha", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("ESTADO DE CUENTA", StringComparison.OrdinalIgnoreCase)) return true;
            if (line.StartsWith("--", StringComparison.OrdinalIgnoreCase)) return true;
            if (Regex.IsMatch(line, @"^\(RD\$\)")) return true;
            return false;
        }

        public static (string? Referencia, string? Descripcion) ExtraerRefDescDeResto(string textoSinMontos)
        {
            textoSinMontos = Regex.Replace(textoSinMontos ?? string.Empty, @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(textoSinMontos))
                return (null, null);

            var refMatch = Regex.Match(
                textoSinMontos,
                @"^(?<ref>[A-Z]{2,10}-?\d{2,})\s*(?<desc>.*)$",
                RegexOptions.IgnoreCase);
            if (refMatch.Success)
            {
                var referencia = refMatch.Groups["ref"].Value.Trim();
                var desc = refMatch.Groups["desc"].Value.Trim();
                return (referencia, string.IsNullOrWhiteSpace(desc) ? null : desc);
            }

            if (textoSinMontos.StartsWith("SALDO", StringComparison.OrdinalIgnoreCase))
                return ("SALDO", textoSinMontos);

            return (null, textoSinMontos);
        }

        public static bool EsDebitoPorReferencia(string? referencia, string? descripcion)
        {
            var t = $"{referencia} {descripcion}".ToUpperInvariant();
            return t.Contains("COM-")
                || t.Contains("COMISION")
                || t.Contains("CARGO")
                || t.Contains("TRF-")
                || t.Contains("CH-")
                || t.Contains("PAGO")
                || t.Contains("IMPUESTO")
                || t.Contains("RETENC");
        }
    }
}
