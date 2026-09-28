using System.Globalization;
using System.Text.RegularExpressions;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Extrae RNC, e-NCF, fechas, totales y líneas de un comprobante RD a partir de texto
    /// (PDF digital o OCR). Sin IA.
    /// </summary>
    public static class FacturaCompraTextoParser
    {
        private static readonly Regex RxNcf = new(
            @"\b(E(?:31|32|33|34|41|43|44|45|46|47)\d{8,13})\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RxRncEmisor = new(
            @"RNC\s*emisor\s*[:\s]+(\d{9,11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RxRnc = new(
            @"\bRNC\b\s*[:\s]+(\d{9,11})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RxFecha = new(
            @"(?<!vencimiento.{0,12})Fecha\s*[:\s]+(\d{1,2}[/\-.]\d{1,2}[/\-.]\d{2,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RxVencimiento = new(
            @"Vencimiento\s*[:\s]+(\d{1,2}[/\-.]\d{1,2}[/\-.]\d{2,4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RxDinero = new(
            @"^\s*(\d{1,3}(?:[.,]\d{3})*[.,]\d{2}|\d+[.,]\d{2})\s*$",
            RegexOptions.Compiled);

        private static readonly Regex RxCantidad = new(
            @"^\s*(\d+(?:[.,]\d{1,3})?)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex RxLineaPlana = new(
            @"^\s*(\d+(?:[.,]\d{1,3})?)\s+(.+?)\s+(\d{1,3}(?:[.,]\d{3})*[.,]\d{2})\s+(\d{1,3}(?:[.,]\d{3})*[.,]\d{2})\s+(\d{1,3}(?:[.,]\d{3})*[.,]\d{2})\s*$",
            RegexOptions.Compiled);

        public static FacturaCompraCamposExtraidos Parse(string? texto)
        {
            var result = new FacturaCompraCamposExtraidos();
            if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 20)
                return result;

            var raw = texto.Replace('\u00a0', ' ');
            var lines = raw
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0)
                .ToArray();
            var blob = string.Join('\n', lines);

            var ncfMatch = RxNcf.Match(blob);
            if (ncfMatch.Success)
                result.Ncf = ncfMatch.Groups[1].Value.ToUpperInvariant();

            var rncEmisor = RxRncEmisor.Match(blob);
            if (rncEmisor.Success)
            {
                result.RncEmisor = rncEmisor.Groups[1].Value;
            }
            else
            {
                foreach (Match m in RxRnc.Matches(blob))
                {
                    var idx = m.Index;
                    var before = blob.Substring(Math.Max(0, idx - 80), Math.Min(80, idx)).ToUpperInvariant();
                    if (before.Contains("COMPRADOR") || before.Contains("CLIENTE") || before.Contains("RECEPTOR"))
                        continue;
                    result.RncEmisor = m.Groups[1].Value;
                    break;
                }
            }

            result.NombreEmisor = ExtraerNombre(lines, result.RncEmisor);

            var fecha = RxFecha.Match(blob);
            if (fecha.Success)
                result.Fecha = NormalizarFecha(fecha.Groups[1].Value);

            var venc = RxVencimiento.Match(blob);
            if (venc.Success)
                result.FechaVencimiento = NormalizarFecha(venc.Groups[1].Value);

            var upper = blob.ToUpperInvariant();
            if (upper.Contains("CREDITO") || upper.Contains("CRÉDITO"))
                result.CondicionPago = "Credito";
            else if (upper.Contains("CONTADO"))
                result.CondicionPago = "Contado";

            ExtraerTotales(lines, result);
            ExtraerLineas(lines, result);

            if (upper.Contains("PRECIOS UNITARIOS SON NETOS") || upper.Contains("SIN ITBIS"))
                result.PreciosIncluyenItbis = false;

            result.Confianza = CalcularConfianza(result);
            return result;
        }

        public static bool EsUtil(FacturaCompraCamposExtraidos x)
        {
            if (x == null) return false;
            if (x.Confianza >= 45) return true;
            if (!string.IsNullOrEmpty(x.Ncf) && (x.Total > 0 || x.Lineas.Count > 0))
                return true;
            if (!string.IsNullOrEmpty(x.RncEmisor) && x.Lineas.Count > 0)
                return true;
            return false;
        }

        private static string? ExtraerNombre(string[] lines, string? rncEmisor)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                if (rncEmisor != null && lines[i].Contains(rncEmisor, StringComparison.Ordinal)
                    && i > 0 && PareceNombreEmpresa(lines[i - 1]))
                    return LimpiarNombre(lines[i - 1]);
            }

            foreach (var line in lines)
            {
                if (PareceNombreEmpresa(line))
                    return LimpiarNombre(line);
            }

            return null;
        }

        private static bool PareceNombreEmpresa(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.Length < 4 || line.Length > 80)
                return false;
            var u = line.ToUpperInvariant();
            if (u.Contains("COMPROBANTE") || u.Contains("FACTURA") || u.Contains("ELECTRONIC")
                || u.StartsWith("RNC") || u.StartsWith("E-NCF") || u.StartsWith("NCF")
                || u.Contains("DGII") || u.Contains("COMPRADOR") || u.StartsWith("TEL")
                || u.StartsWith("CALLE") || u.Contains("DESCRIPCION") || u.Contains("SUBTOTAL"))
                return false;
            var letters = line.Count(char.IsLetter);
            if (letters < 6) return false;
            return u.Contains("SRL") || u.Contains("S.A") || u.Contains("SAS")
                || u.Contains("EIRL") || letters >= 10;
        }

        private static string LimpiarNombre(string line) =>
            Regex.Replace(line, @"\s+", " ").Trim();

        private static void ExtraerTotales(string[] lines, FacturaCompraCamposExtraidos result)
        {
            for (var i = 0; i < lines.Length; i++)
            {
                var u = lines[i].ToUpperInvariant();
                if (TryMontoSiguiente(lines, i, u, "SUBTOTAL", out var sub))
                    result.Subtotal = sub;
                if (TryMontoSiguiente(lines, i, u, "ITBIS", out var itbis)
                    && !u.Contains("UNITARIO") && result.Itbis == 0)
                    result.Itbis = itbis;
                if ((u.Contains("TOTAL") && !u.Contains("SUBTOTAL") && !u.Contains("UNITARIO"))
                    && TryMontoSiguiente(lines, i, u, "TOTAL", out var tot))
                    result.Total = tot;
            }
        }

        private static bool TryMontoSiguiente(
            string[] lines,
            int i,
            string upperLine,
            string etiqueta,
            out decimal monto)
        {
            monto = 0;
            if (!upperLine.Contains(etiqueta))
                return false;

            if (TryParseDinero(QuitarEtiqueta(lines[i]), out monto) && monto > 0)
                return true;

            if (i + 1 < lines.Length && TryParseDinero(lines[i + 1], out monto) && monto > 0)
                return true;

            return false;
        }

        private static string QuitarEtiqueta(string line) =>
            Regex.Replace(line, @"(?i)subtotal|itbis\s*\d*\s*%?|total(\s*rd\$)?|importe", "").Trim();

        private static void ExtraerLineas(string[] lines, FacturaCompraCamposExtraidos result)
        {
            var start = 0;
            for (var i = 0; i < lines.Length; i++)
            {
                var u = lines[i].ToUpperInvariant();
                if (u.Contains("DESCRIPCION") || u == "CANT." || u == "IMPORTE")
                    start = i + 1;
                if (u.StartsWith("SUBTOTAL"))
                {
                    ParseBloqueLineas(lines.Skip(start).Take(Math.Max(0, i - start)).ToArray(), result);
                    return;
                }
            }

            ParseBloqueLineas(lines.Skip(start).ToArray(), result);
        }

        private static void ParseBloqueLineas(string[] block, FacturaCompraCamposExtraidos result)
        {
            for (var i = 0; i < block.Length; i++)
            {
                var plana = RxLineaPlana.Match(block[i]);
                if (plana.Success)
                {
                    AgregarLinea(
                        result,
                        plana.Groups[2].Value,
                        plana.Groups[1].Value,
                        plana.Groups[3].Value,
                        plana.Groups[4].Value,
                        plana.Groups[5].Value);
                    continue;
                }

                if (i + 4 < block.Length
                    && EsCantidadLinea(block[i])
                    && !EsDineroLinea(block[i + 1]) && block[i + 1].Any(char.IsLetter)
                    && EsDineroLinea(block[i + 2])
                    && EsDineroLinea(block[i + 3])
                    && EsDineroLinea(block[i + 4]))
                {
                    AgregarLinea(result, block[i + 1], block[i], block[i + 2], block[i + 3], block[i + 4]);
                    i += 4;
                }
            }
        }

        private static void AgregarLinea(
            FacturaCompraCamposExtraidos result,
            string desc,
            string cant,
            string pu,
            string itbis,
            string imp)
        {
            if (!TryParseDinero(cant, out var cantidad) && !decimal.TryParse(cant.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out cantidad))
                cantidad = 1;
            TryParseDinero(pu, out var precio);
            TryParseDinero(itbis, out var itb);
            TryParseDinero(imp, out var importe);
            if (precio <= 0 && importe <= 0)
                return;

            result.Lineas.Add(new FacturaCompraLineaExtraida
            {
                Descripcion = desc.Trim(),
                Cantidad = cantidad <= 0 ? 1 : cantidad,
                PrecioUnitario = precio,
                Itbis = itb,
                Importe = importe
            });
        }

        private static bool EsDineroLinea(string s) => RxDinero.IsMatch(s);

        private static bool EsCantidadLinea(string s)
        {
            if (!RxCantidad.IsMatch(s)) return false;
            if (!decimal.TryParse(s.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
                return false;
            return v > 0 && v < 100000;
        }

        private static bool TryParseDinero(string raw, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim().Replace(" ", "").Replace("RD$", "", StringComparison.OrdinalIgnoreCase).Replace("$", "");
            if (s.Contains(',') && s.Contains('.'))
            {
                if (s.LastIndexOf(',') > s.LastIndexOf('.'))
                    s = s.Replace(".", "").Replace(',', '.');
                else
                    s = s.Replace(",", "");
            }
            else if (s.Contains(',') && !s.Contains('.'))
            {
                var parts = s.Split(',');
                if (parts.Length == 2 && parts[1].Length == 2)
                    s = parts[0] + "." + parts[1];
                else
                    s = s.Replace(",", "");
            }

            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private static string? NormalizarFecha(string raw)
        {
            var s = raw.Trim().Replace('-', '/').Replace('.', '/');
            if (DateTime.TryParse(s, new CultureInfo("es-DO"), DateTimeStyles.None, out var d))
                return d.ToString("yyyy-MM-dd");
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
                return d.ToString("yyyy-MM-dd");
            return null;
        }

        private static int CalcularConfianza(FacturaCompraCamposExtraidos x)
        {
            var n = 0;
            if (!string.IsNullOrEmpty(x.Ncf)) n += 30;
            if (!string.IsNullOrEmpty(x.RncEmisor)) n += 20;
            if (!string.IsNullOrEmpty(x.NombreEmisor)) n += 8;
            if (!string.IsNullOrEmpty(x.Fecha)) n += 10;
            if (x.Total > 0) n += 12;
            if (x.Subtotal > 0) n += 5;
            n += Math.Min(15, x.Lineas.Count * 5);
            return Math.Min(100, n);
        }
    }

    public sealed class FacturaCompraCamposExtraidos
    {
        public string? RncEmisor { get; set; }
        public string? NombreEmisor { get; set; }
        public string? Ncf { get; set; }
        public string? Fecha { get; set; }
        public string? CondicionPago { get; set; }
        public string? FechaVencimiento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public bool PreciosIncluyenItbis { get; set; }
        public List<FacturaCompraLineaExtraida> Lineas { get; set; } = new();
        public int Confianza { get; set; }
    }

    public sealed class FacturaCompraLineaExtraida
    {
        public string? Descripcion { get; set; }
        public string? Codigo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Itbis { get; set; }
        public decimal Importe { get; set; }
    }
}
