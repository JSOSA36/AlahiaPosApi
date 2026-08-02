using System.Globalization;

namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Parser genérico CSV/TXT (con o sin encabezado).
    /// Comportamiento portado sin cambios desde TesoreriaExtractoService.
    /// </summary>
    public sealed class CsvStatementAdapter : IBankStatementAdapter
    {
        public const string NombreAdapter = "CSV_GENERICO";

        public string Nombre => NombreAdapter;

        public bool CanParse(BankStatementParseRequest request)
        {
            var formato = (request.Formato ?? string.Empty).Trim().ToUpperInvariant();
            return formato is not ("XLSX" or "XLS" or "PDF");
        }

        public BankStatementParseResult Parse(BankStatementParseRequest request)
        {
            var csv = !string.IsNullOrWhiteSpace(request.ContenidoCsv)
                ? request.ContenidoCsv
                : request.TextoExtraido;

            if (string.IsNullOrWhiteSpace(csv))
                throw new InvalidOperationException("Contenido vacío.");

            return ParseCsv(csv);
        }

        internal static BankStatementParseResult ParseCsv(string contenido)
        {
            var result = new BankStatementParseResult();
            using var reader = new StringReader(contenido);
            var header = reader.ReadLine();
            var cultura = ExtractoParsingHelpers.CulturaDo;

            // Detectar columnas por encabezado si existe.
            Dictionary<string, int>? map = null;
            if (!string.IsNullOrWhiteSpace(header)
                && !ExtractoParsingHelpers.TryParseFecha(ExtractoParsingHelpers.SplitCsvLine(header)[0], cultura, out _))
            {
                map = ExtractoParsingHelpers.MapHeaders(ExtractoParsingHelpers.SplitCsvLine(header));
            }
            else if (!string.IsNullOrWhiteSpace(header))
            {
                // Primera línea es data.
                reader.Dispose();
                using var reader2 = new StringReader(contenido);
                ParseCsvRows(reader2, null, cultura, result);
                CompletarTotales(result);
                return result;
            }

            ParseCsvRows(reader, map, cultura, result);
            CompletarTotales(result);
            return result;
        }

        private static void CompletarTotales(BankStatementParseResult result)
        {
            if (result.Lineas.Count == 0)
                return;

            result.PeriodoDesde ??= result.Lineas.Min(x => x.Fecha);
            result.PeriodoHasta ??= result.Lineas.Max(x => x.Fecha);
        }

        private static void ParseCsvRows(
            TextReader reader,
            Dictionary<string, int>? map,
            CultureInfo cultura,
            BankStatementParseResult result)
        {
            var filasOmitidas = 0;
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = ExtractoParsingHelpers.SplitCsvLine(line);
                if (cols.Count < 2) continue;

                string fechaRaw, desc, referencia, debRaw, credRaw, balRaw;
                if (map != null)
                {
                    fechaRaw = ExtractoParsingHelpers.GetCol(cols, map, "fecha");
                    desc = ExtractoParsingHelpers.GetCol(cols, map, "descripcion");
                    referencia = ExtractoParsingHelpers.GetCol(cols, map, "referencia");
                    debRaw = ExtractoParsingHelpers.GetCol(cols, map, "debito");
                    credRaw = ExtractoParsingHelpers.GetCol(cols, map, "credito");
                    balRaw = ExtractoParsingHelpers.GetCol(cols, map, "balance");
                }
                else
                {
                    // fecha, descripcion, referencia, debito, credito [, balance]
                    // o fecha, referencia, descripcion, debito, credito, balance
                    fechaRaw = cols[0];
                    if (cols.Count >= 6)
                    {
                        referencia = cols[1];
                        desc = cols[2];
                        debRaw = cols[3];
                        credRaw = cols[4];
                        balRaw = cols[5];
                    }
                    else
                    {
                        desc = cols.ElementAtOrDefault(1) ?? "";
                        referencia = cols.ElementAtOrDefault(2) ?? "";
                        debRaw = cols.ElementAtOrDefault(3) ?? "";
                        credRaw = cols.ElementAtOrDefault(4) ?? "";
                        balRaw = cols.ElementAtOrDefault(5) ?? "";
                    }
                }

                if (!ExtractoParsingHelpers.TryParseFecha(fechaRaw, cultura, out var fecha))
                {
                    filasOmitidas++;
                    continue;
                }

                var debito = ExtractoParsingHelpers.ParseDecimal(debRaw, cultura);
                var credito = ExtractoParsingHelpers.ParseDecimal(credRaw, cultura);
                var balance = string.IsNullOrWhiteSpace(balRaw)
                    ? (decimal?)null
                    : ExtractoParsingHelpers.ParseDecimal(balRaw, cultura);

                if (debito == 0 && credito == 0)
                {
                    filasOmitidas++;
                    continue;
                }

                result.Lineas.Add(new BankStatementLine
                {
                    Fecha = fecha,
                    Descripcion = desc,
                    Referencia = referencia,
                    Debito = Math.Abs(debito),
                    Credito = Math.Abs(credito),
                    Balance = balance
                });
            }

            if (filasOmitidas > 0)
                result.Warnings.Add(
                    $"Se omitieron {filasOmitidas} fila(s) sin fecha válida o sin montos.");
        }
    }
}
