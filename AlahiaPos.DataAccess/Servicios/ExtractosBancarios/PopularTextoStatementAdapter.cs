using System.Text.RegularExpressions;

namespace AlahiaPos.DataAccess.Servicios.ExtractosBancarios
{
    /// <summary>
    /// Parser del estado de cuenta Popular (PDF/texto) donde débito/crédito
    /// colapsan y se infieren por la evolución del balance.
    /// Comportamiento portado sin cambios desde TesoreriaExtractoService.
    /// </summary>
    public sealed class PopularTextoStatementAdapter : IBankStatementAdapter
    {
        public const string NombreAdapter = "POPULAR_TEXTO";

        public string Nombre => NombreAdapter;

        public bool CanParse(BankStatementParseRequest request)
        {
            var formato = (request.Formato ?? string.Empty).Trim().ToUpperInvariant();
            if (formato == "PDF")
                return true;

            if (formato is "XLSX" or "XLS")
                return false;

            var texto = !string.IsNullOrWhiteSpace(request.ContenidoCsv)
                ? request.ContenidoCsv
                : request.TextoExtraido;

            return !string.IsNullOrWhiteSpace(texto)
                && (texto.Contains("ESTADO DE CUENTA", StringComparison.OrdinalIgnoreCase)
                    || texto.Contains("Banco:", StringComparison.OrdinalIgnoreCase));
        }

        public BankStatementParseResult Parse(BankStatementParseRequest request)
        {
            string? texto;
            var formato = (request.Formato ?? string.Empty).Trim().ToUpperInvariant();
            if (formato == "PDF")
            {
                texto = request.TextoExtraido;
                if (string.IsNullOrWhiteSpace(texto) && !string.IsNullOrWhiteSpace(request.ContenidoCsv))
                    texto = request.ContenidoCsv;
                if (string.IsNullOrWhiteSpace(texto))
                    throw new InvalidOperationException("No se recibió texto extraído del PDF.");
            }
            else
            {
                texto = !string.IsNullOrWhiteSpace(request.ContenidoCsv)
                    ? request.ContenidoCsv
                    : request.TextoExtraido;
                if (string.IsNullOrWhiteSpace(texto))
                    throw new InvalidOperationException("Contenido vacío.");
            }

            return ParseEstadoCuentaTexto(texto);
        }

        internal static BankStatementParseResult ParseEstadoCuentaTexto(string texto)
        {
            var result = new BankStatementParseResult();
            var cultura = ExtractoParsingHelpers.CulturaDo;
            var lines = texto
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => Regex.Replace(l.Trim(), @"\s+", " "))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            foreach (var rawLine in lines)
            {
                var line = ExtractoParsingHelpers.StripBulletPrefix(rawLine);

                if (line.StartsWith("Banco:", StringComparison.OrdinalIgnoreCase))
                    result.Banco = ExtractoParsingHelpers.ExtractAfter(line, "Banco:");
                else if (line.StartsWith("Cuenta:", StringComparison.OrdinalIgnoreCase))
                    result.NumeroCuentaBanco = ExtractoParsingHelpers.ExtractAfter(line, "Cuenta:");
                else if (line.StartsWith("Moneda:", StringComparison.OrdinalIgnoreCase))
                    result.Moneda = ExtractoParsingHelpers.ExtractAfter(line, "Moneda:").Split('(', ' ')[0].Trim();
                else if (line.StartsWith("Período:", StringComparison.OrdinalIgnoreCase)
                         || line.StartsWith("Periodo:", StringComparison.OrdinalIgnoreCase))
                {
                    var periodo = ExtractoParsingHelpers.ExtractAfter(line, line.Contains('í') ? "Período:" : "Periodo:");
                    var partes = periodo.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (partes.Length >= 2)
                    {
                        if (ExtractoParsingHelpers.TryParseFecha(partes[0], cultura, out var d)) result.PeriodoDesde = d;
                        if (ExtractoParsingHelpers.TryParseFecha(partes[1], cultura, out var h)) result.PeriodoHasta = h;
                    }
                }
                else if (line.StartsWith("Saldo Inicial:", StringComparison.OrdinalIgnoreCase))
                    result.SaldoInicial = ExtractoParsingHelpers.ParseDecimal(
                        ExtractoParsingHelpers.ExtractAfter(line, "Saldo Inicial:"), cultura);
                else if (line.StartsWith("Total Créditos:", StringComparison.OrdinalIgnoreCase)
                         || line.StartsWith("Total Creditos:", StringComparison.OrdinalIgnoreCase))
                    result.TotalCreditos = ExtractoParsingHelpers.ParseDecimal(
                        ExtractoParsingHelpers.ExtractAfter(line, ":"), cultura);
                else if (line.StartsWith("Total Débitos:", StringComparison.OrdinalIgnoreCase)
                         || line.StartsWith("Total Debitos:", StringComparison.OrdinalIgnoreCase))
                    result.TotalDebitos = ExtractoParsingHelpers.ParseDecimal(
                        ExtractoParsingHelpers.ExtractAfter(line, ":"), cultura);
                else if (line.StartsWith("Saldo Final:", StringComparison.OrdinalIgnoreCase))
                    result.SaldoFinal = ExtractoParsingHelpers.ParseDecimal(
                        ExtractoParsingHelpers.ExtractAfter(line, "Saldo Final:"), cultura);
            }

            // Reordenar texto PDF Popular: las descripciones huérfanas entre filas
            // fechadas pertenecen a la fila siguiente si esta no trae descripción;
            // si ya tiene, continúan la fila anterior.
            var moneyRx = new Regex(@"-?\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})|-?\d+(?:[.,]\d{2})", RegexOptions.Compiled);
            var datedRows = new List<(string Line, List<string> Prefijos, List<string> Sufijos)>();
            var pendingOrphans = new List<string>();

            foreach (var rawLine in lines)
            {
                var line = ExtractoParsingHelpers.StripBulletPrefix(rawLine);
                if (ExtractoParsingHelpers.EsLineaMetaExtracto(line))
                    continue;

                if (Regex.IsMatch(line, @"^\d{2}/\d{2}/\d{4}\b"))
                {
                    datedRows.Add((line, new List<string>(pendingOrphans), new List<string>()));
                    pendingOrphans.Clear();
                }
                else if (datedRows.Count > 0)
                {
                    pendingOrphans.Add(line);
                }
            }

            for (var i = 0; i < datedRows.Count; i++)
            {
                var row = datedRows[i];
                if (row.Prefijos.Count == 0)
                    continue;

                var (_, descPropia) = ExtractoParsingHelpers.ExtraerRefDescDeResto(
                    moneyRx.Replace(Regex.Replace(row.Line, @"^\d{2}/\d{2}/\d{4}\s+", ""), " "));
                var tieneDescPropia = !string.IsNullOrWhiteSpace(descPropia);

                if (!tieneDescPropia)
                {
                    // Prefijos ya están en esta fila (correcto).
                    continue;
                }

                if (i > 0)
                {
                    datedRows[i - 1].Sufijos.AddRange(row.Prefijos);
                    row.Prefijos.Clear();
                    datedRows[i] = row;
                }
            }

            if (pendingOrphans.Count > 0 && datedRows.Count > 0)
                datedRows[^1].Sufijos.AddRange(pendingOrphans);

            decimal? saldoCorrido = result.SaldoInicial;
            var lineasInferidasPorPalabras = 0;

            foreach (var row in datedRows)
            {
                var m = Regex.Match(row.Line, @"^(?<fecha>\d{2}/\d{2}/\d{4})\s+(?<resto>.+)$");
                if (!m.Success) continue;

                if (!ExtractoParsingHelpers.TryParseFecha(m.Groups["fecha"].Value, cultura, out var fecha))
                    continue;

                var resto = m.Groups["resto"].Value.Trim();
                var moneyMatches = moneyRx.Matches(resto);
                if (moneyMatches.Count == 0) continue;

                // Solo montos de la fila fechada (no de texto de resumen anexado).
                var montos = moneyMatches
                    .Cast<Match>()
                    .Select(x => ExtractoParsingHelpers.ParseDecimal(x.Value, cultura))
                    .ToList();

                var textoSinMontos = resto;
                foreach (Match mm in moneyMatches)
                    textoSinMontos = textoSinMontos.Replace(mm.Value, " ");
                textoSinMontos = Regex.Replace(textoSinMontos, @"\s+", " ").Trim();

                var (referencia, descripcion) = ExtractoParsingHelpers.ExtraerRefDescDeResto(textoSinMontos);

                var extras = row.Prefijos.Concat(row.Sufijos)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
                if (extras.Count > 0)
                {
                    var extraTxt = string.Join(" ", extras);
                    descripcion = string.IsNullOrWhiteSpace(descripcion)
                        ? extraTxt
                        : $"{descripcion} {extraTxt}".Trim();
                }

                decimal? balance = null;
                decimal montoMov = 0;
                if (montos.Count >= 2)
                {
                    balance = montos[^1];
                    montoMov = Math.Abs(montos[^2]);
                }
                else
                {
                    montoMov = Math.Abs(montos[0]);
                }

                if (string.Equals(referencia, "SALDO", StringComparison.OrdinalIgnoreCase)
                    || (string.IsNullOrWhiteSpace(referencia)
                        && (descripcion?.Contains("Saldo inicial", StringComparison.OrdinalIgnoreCase) ?? false)))
                {
                    result.SaldoInicial ??= balance ?? montoMov;
                    saldoCorrido = result.SaldoInicial;
                    continue;
                }

                decimal debito = 0, credito = 0;
                if (balance.HasValue && saldoCorrido.HasValue)
                {
                    if (balance.Value > saldoCorrido.Value) credito = montoMov;
                    else if (balance.Value < saldoCorrido.Value) debito = montoMov;
                    else
                    {
                        if (ExtractoParsingHelpers.EsDebitoPorReferencia(referencia, descripcion)) debito = montoMov;
                        else credito = montoMov;
                        lineasInferidasPorPalabras++;
                    }
                }
                else
                {
                    if (ExtractoParsingHelpers.EsDebitoPorReferencia(referencia, descripcion)) debito = montoMov;
                    else credito = montoMov;
                    lineasInferidasPorPalabras++;
                }

                if (debito == 0 && credito == 0) continue;

                result.Lineas.Add(new BankStatementLine
                {
                    Fecha = fecha,
                    Referencia = referencia,
                    Descripcion = descripcion,
                    Debito = debito,
                    Credito = credito,
                    Balance = balance
                });

                if (balance.HasValue)
                    saldoCorrido = balance;
                else if (saldoCorrido.HasValue)
                    saldoCorrido = saldoCorrido + credito - debito;
            }

            var totalDebitosDeclarado = result.TotalDebitos;
            var totalCreditosDeclarado = result.TotalCreditos;

            if (result.Lineas.Count > 0)
            {
                result.PeriodoDesde ??= result.Lineas.Min(x => x.Fecha);
                result.PeriodoHasta ??= result.Lineas.Max(x => x.Fecha);
                if (result.TotalDebitos <= 0) result.TotalDebitos = result.Lineas.Sum(x => x.Debito);
                if (result.TotalCreditos <= 0) result.TotalCreditos = result.Lineas.Sum(x => x.Credito);
                result.SaldoFinal ??= result.Lineas.LastOrDefault(x => x.Balance.HasValue)?.Balance
                    ?? (result.SaldoInicial.HasValue
                        ? result.SaldoInicial + result.TotalCreditos - result.TotalDebitos
                        : null);
            }

            // Advertencias no bloqueantes para el preview editable.
            if (result.SaldoInicial is null)
                result.Warnings.Add(
                    "No se encontró saldo inicial; la dirección débito/crédito se infirió por palabras clave.");

            if (lineasInferidasPorPalabras > 0)
                result.Warnings.Add(
                    $"{lineasInferidasPorPalabras} línea(s) sin balance comparable: dirección inferida por palabras clave. Revise antes de confirmar.");

            if (totalDebitosDeclarado > 0)
            {
                var calc = result.Lineas.Sum(x => x.Debito);
                if (calc != totalDebitosDeclarado)
                    result.Warnings.Add(
                        $"Total débitos declarado ({totalDebitosDeclarado:N2}) difiere de la suma de líneas ({calc:N2}).");
            }

            if (totalCreditosDeclarado > 0)
            {
                var calc = result.Lineas.Sum(x => x.Credito);
                if (calc != totalCreditosDeclarado)
                    result.Warnings.Add(
                        $"Total créditos declarado ({totalCreditosDeclarado:N2}) difiere de la suma de líneas ({calc:N2}).");
            }

            return result;
        }
    }
}
