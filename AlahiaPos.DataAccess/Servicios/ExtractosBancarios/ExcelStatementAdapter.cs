using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;

namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Parser Excel: XLSX vía ClosedXML y XLS legacy vía ExcelDataReader.
    /// Comportamiento portado sin cambios desde TesoreriaExtractoService.
    /// </summary>
    public sealed class ExcelStatementAdapter : IBankStatementAdapter
    {
        public const string NombreAdapter = "EXCEL";

        public string Nombre => NombreAdapter;

        public bool CanParse(BankStatementParseRequest request)
        {
            var formato = (request.Formato ?? string.Empty).Trim().ToUpperInvariant();
            return formato is "XLSX" or "XLS";
        }

        public BankStatementParseResult Parse(BankStatementParseRequest request)
        {
            if (request.ContenidoArchivo is not { Length: > 0 })
                throw new InvalidOperationException("Archivo Excel vacío.");

            var formato = (request.Formato ?? string.Empty).Trim().ToUpperInvariant();
            using var ms = new MemoryStream(request.ContenidoArchivo);
            return formato == "XLS" ? ParseLegacyExcel(ms) : ParseExcel(ms);
        }

        internal static BankStatementParseResult ParseExcel(Stream stream)
        {
            var result = new BankStatementParseResult();
            var cultura = ExtractoParsingHelpers.CulturaDo;

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var used = ws.RangeUsed();
            if (used == null)
                return result;

            var rows = used.RowsUsed().ToList();
            if (rows.Count == 0)
                return result;

            // Buscar fila de encabezado.
            Dictionary<string, int>? map = null;
            var dataStart = 0;
            for (var i = 0; i < Math.Min(15, rows.Count); i++)
            {
                var values = rows[i].Cells().Select(c => c.GetString().Trim()).ToList();
                var candidate = ExtractoParsingHelpers.MapHeaders(values);
                if (candidate.ContainsKey("fecha")
                    && (candidate.ContainsKey("debito") || candidate.ContainsKey("credito") || candidate.ContainsKey("balance")))
                {
                    map = candidate;
                    dataStart = i + 1;
                    break;
                }
            }

            var filasOmitidas = 0;
            for (var i = dataStart; i < rows.Count; i++)
            {
                var cells = rows[i].Cells().Select(c =>
                {
                    if (c.DataType == XLDataType.DateTime)
                        return c.GetDateTime().ToString("dd/MM/yyyy", cultura);
                    if (c.DataType == XLDataType.Number)
                        return c.GetDouble().ToString(cultura);
                    return c.GetString().Trim();
                }).ToList();

                if (cells.Count == 0 || cells.All(string.IsNullOrWhiteSpace))
                    continue;

                // Metadata rows
                var joined = string.Join(" ", cells);
                if (joined.StartsWith("Banco:", StringComparison.OrdinalIgnoreCase))
                    result.Banco = cells.Skip(1).FirstOrDefault() ?? ExtractoParsingHelpers.ExtractAfter(joined, "Banco:");
                if (joined.StartsWith("Cuenta:", StringComparison.OrdinalIgnoreCase))
                    result.NumeroCuentaBanco = cells.Skip(1).FirstOrDefault() ?? ExtractoParsingHelpers.ExtractAfter(joined, "Cuenta:");
                if (joined.StartsWith("Moneda:", StringComparison.OrdinalIgnoreCase))
                    result.Moneda = (cells.Skip(1).FirstOrDefault() ?? ExtractoParsingHelpers.ExtractAfter(joined, "Moneda:")).Split(' ')[0];

                string fechaRaw, desc, referencia, debRaw, credRaw, balRaw;
                if (map != null)
                {
                    fechaRaw = ExtractoParsingHelpers.GetCol(cells, map, "fecha");
                    desc = ExtractoParsingHelpers.GetCol(cells, map, "descripcion");
                    referencia = ExtractoParsingHelpers.GetCol(cells, map, "referencia");
                    debRaw = ExtractoParsingHelpers.GetCol(cells, map, "debito");
                    credRaw = ExtractoParsingHelpers.GetCol(cells, map, "credito");
                    balRaw = ExtractoParsingHelpers.GetCol(cells, map, "balance");
                }
                else
                {
                    fechaRaw = cells.ElementAtOrDefault(0) ?? "";
                    referencia = cells.ElementAtOrDefault(1) ?? "";
                    desc = cells.ElementAtOrDefault(2) ?? "";
                    debRaw = cells.ElementAtOrDefault(3) ?? "";
                    credRaw = cells.ElementAtOrDefault(4) ?? "";
                    balRaw = cells.ElementAtOrDefault(5) ?? "";
                }

                if (!ExtractoParsingHelpers.TryParseFecha(fechaRaw, cultura, out var fecha))
                {
                    filasOmitidas++;
                    continue;
                }

                var debito = ExtractoParsingHelpers.ParseDecimal(debRaw, cultura);
                var credito = ExtractoParsingHelpers.ParseDecimal(credRaw, cultura);
                decimal? balance = string.IsNullOrWhiteSpace(balRaw)
                    ? null
                    : ExtractoParsingHelpers.ParseDecimal(balRaw, cultura);

                // Si solo hay un monto + balance, inferir.
                if (debito == 0 && credito == 0)
                {
                    // Buscar montos numéricos en celdas.
                    var nums = cells
                        .Select(c => ExtractoParsingHelpers.ParseDecimal(c, cultura))
                        .Where(n => n != 0)
                        .ToList();
                    if (nums.Count >= 1)
                    {
                        // Heurística: última = balance, penúltima = monto
                        if (nums.Count >= 2)
                        {
                            balance ??= nums[^1];
                            var monto = Math.Abs(nums[^2]);
                            // Inferencia requiere saldo previo; se deja pendiente si no hay.
                            if (result.Lineas.Count > 0 && result.Lineas[^1].Balance is decimal prev)
                            {
                                if (balance > prev) credito = monto;
                                else debito = monto;
                            }
                            else credito = monto;
                        }
                    }
                }

                if (debito == 0 && credito == 0)
                {
                    filasOmitidas++;
                    continue;
                }

                // Skip saldo inicial
                if ((desc + " " + referencia).Contains("Saldo inicial", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(referencia, "SALDO", StringComparison.OrdinalIgnoreCase))
                {
                    result.SaldoInicial ??= balance ?? debito + credito;
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

            if (result.Lineas.Count > 0)
            {
                result.PeriodoDesde ??= result.Lineas.Min(x => x.Fecha);
                result.PeriodoHasta ??= result.Lineas.Max(x => x.Fecha);
                result.TotalDebitos = result.Lineas.Sum(x => x.Debito);
                result.TotalCreditos = result.Lineas.Sum(x => x.Credito);
                result.SaldoFinal ??= result.Lineas.LastOrDefault(x => x.Balance.HasValue)?.Balance;
            }

            if (filasOmitidas > 0)
                result.Warnings.Add($"Se omitieron {filasOmitidas} fila(s) sin fecha válida o sin montos.");

            return result;
        }

        internal static BankStatementParseResult ParseLegacyExcel(Stream stream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
            {
                FallbackEncoding = Encoding.GetEncoding(1252),
                LeaveOpen = false
            });
            var rows = new List<List<string>>();
            while (reader.Read())
            {
                var cells = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    cells.Add(value switch
                    {
                        DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                        double number => number.ToString(CultureInfo.InvariantCulture),
                        _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
                    });
                }
                rows.Add(cells);
            }

            var result = new BankStatementParseResult();
            var cultura = ExtractoParsingHelpers.CulturaDo;
            var headerIndex = rows.FindIndex(x =>
            {
                var candidate = ExtractoParsingHelpers.MapHeaders(x);
                return candidate.ContainsKey("fecha")
                    && (candidate.ContainsKey("debito") || candidate.ContainsKey("credito"));
            });
            if (headerIndex < 0)
                throw new InvalidOperationException("No se encontró el encabezado de movimientos en el archivo XLS.");

            var map = ExtractoParsingHelpers.MapHeaders(rows[headerIndex]);
            decimal? saldoAnterior = null;
            var filasOmitidas = 0;
            foreach (var cells in rows.Skip(headerIndex + 1))
            {
                var fechaRaw = ExtractoParsingHelpers.GetCol(cells, map, "fecha");
                if (!ExtractoParsingHelpers.TryParseFecha(fechaRaw, cultura, out var fecha))
                {
                    filasOmitidas++;
                    continue;
                }
                var descripcion = ExtractoParsingHelpers.GetCol(cells, map, "descripcion");
                var referencia = ExtractoParsingHelpers.GetCol(cells, map, "referencia");
                var debito = Math.Abs(ExtractoParsingHelpers.ParseDecimal(
                    ExtractoParsingHelpers.GetCol(cells, map, "debito"), cultura));
                var credito = Math.Abs(ExtractoParsingHelpers.ParseDecimal(
                    ExtractoParsingHelpers.GetCol(cells, map, "credito"), cultura));
                var balanceRaw = ExtractoParsingHelpers.GetCol(cells, map, "balance");
                var balance = string.IsNullOrWhiteSpace(balanceRaw)
                    ? (decimal?)null
                    : ExtractoParsingHelpers.ParseDecimal(balanceRaw, cultura);

                if (string.Equals(referencia, "SALDO", StringComparison.OrdinalIgnoreCase)
                    || descripcion.Contains("Saldo inicial", StringComparison.OrdinalIgnoreCase))
                {
                    result.SaldoInicial = balance ?? debito + credito;
                    saldoAnterior = result.SaldoInicial;
                    continue;
                }

                if (debito == 0 && credito == 0 && balance.HasValue && saldoAnterior.HasValue)
                {
                    var delta = balance.Value - saldoAnterior.Value;
                    if (delta < 0) debito = Math.Abs(delta);
                    else credito = delta;
                }
                if (debito == 0 && credito == 0)
                {
                    filasOmitidas++;
                    continue;
                }

                result.Lineas.Add(new BankStatementLine
                {
                    Fecha = fecha,
                    Descripcion = descripcion,
                    Referencia = referencia,
                    Debito = debito,
                    Credito = credito,
                    Balance = balance
                });
                saldoAnterior = balance ?? saldoAnterior + credito - debito;
            }

            if (result.Lineas.Count > 0)
            {
                result.PeriodoDesde = result.Lineas.Min(x => x.Fecha);
                result.PeriodoHasta = result.Lineas.Max(x => x.Fecha);
                result.TotalDebitos = result.Lineas.Sum(x => x.Debito);
                result.TotalCreditos = result.Lineas.Sum(x => x.Credito);
                result.SaldoFinal = result.Lineas.LastOrDefault(x => x.Balance.HasValue)?.Balance;
            }

            if (filasOmitidas > 0)
                result.Warnings.Add($"Se omitieron {filasOmitidas} fila(s) sin fecha válida o sin montos.");

            return result;
        }
    }
}
