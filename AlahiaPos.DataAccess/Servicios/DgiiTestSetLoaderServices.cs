using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using ClosedXML.Excel;
using System.Globalization;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DgiiTestSetLoaderServices : ITestSetLoader
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public (List<EcfRow> ecfs, List<RfceRow> rfces) Cargar(string pathXlsx)
        {
            using var workbook = new XLWorkbook(pathXlsx);

            var ecfSheet = workbook.Worksheet("ECF");
            var rfceSheet = workbook.Worksheet("RFCE");

            var ecfs = CargarEcf(ecfSheet);
            var rfces = CargarRfce(rfceSheet);

            return (ecfs, rfces);
        }

        private List<EcfRow> CargarEcf(IXLWorksheet sheet)
        {
            var headers = ObtenerMapaColumnas(sheet);
            var lastRow = sheet.LastRowUsed().RowNumber();
            var lista = new List<EcfRow>();

            for (int row = 2; row <= lastRow; row++)
            {
                var tipo = Obtener(sheet, row, headers, "TipoeCF");
                if (string.IsNullOrWhiteSpace(tipo)) continue;

                var encf = Obtener(sheet, row, headers, "ENCF");
                var rncEmisor = Obtener(sheet, row, headers, "RNCEmisor");
                var fecha = ParseDate(Obtener(sheet, row, headers, "FechaEmision"));

                var rncComprador = Obtener(sheet, row, headers, "RNCComprador");
                var razonComprador = Obtener(sheet, row, headers, "RazonSocialComprador");

                var montoGravado = ParseDecimal(Obtener(sheet, row, headers, "MontoGravadoTotal"));
                var totalItbis = ParseDecimal(Obtener(sheet, row, headers, "TotalITBIS"));
                var montoTotal = ParseDecimal(Obtener(sheet, row, headers, "MontoTotal"));

                lista.Add(new EcfRow(
                    tipo,
                    encf,
                    rncEmisor,
                    fecha,
                    rncComprador,
                    razonComprador,
                    montoGravado,
                    totalItbis,
                    montoTotal
                ));
            }

            return lista;
        }

        private List<RfceRow> CargarRfce(IXLWorksheet sheet)
        {
            var headers = ObtenerMapaColumnas(sheet);
            var lastRow = sheet.LastRowUsed().RowNumber();
            var lista = new List<RfceRow>();

            for (int row = 2; row <= lastRow; row++)
            {
                var encf = Obtener(sheet, row, headers, "ENCF");
                if (string.IsNullOrWhiteSpace(encf)) continue;

                var rncEmisor = Obtener(sheet, row, headers, "RNCEmisor");
                var fecha = ParseDate(Obtener(sheet, row, headers, "FechaEmision"));
                var rncComprador = Obtener(sheet, row, headers, "RNCComprador");

                var montoGravado = ParseDecimal(Obtener(sheet, row, headers, "MontoGravadoTotal"));
                var totalItbis = ParseDecimal(Obtener(sheet, row, headers, "TotalITBIS"));
                var montoTotal = ParseDecimal(Obtener(sheet, row, headers, "MontoTotal"));

                lista.Add(new RfceRow(
     encf,
     rncEmisor,
     "MACROBITS SRL",          // RazonSocialEmisor
     fecha,
     rncComprador,
     null,                     // IdentificadorExtranjero
     null,                     // RazonSocialComprador
     montoGravado,
     totalItbis,
     montoTotal,
     "01",                     // TipoIngresos
     "1",                      // TipoPago
     null                      // FormasPago
 ));
            }

            return lista;
        }

        private Dictionary<string, int> ObtenerMapaColumnas(IXLWorksheet sheet)
        {
            var mapa = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in sheet.Row(1).CellsUsed())
            {
                var nombre = cell.GetString().Trim();
                if (!string.IsNullOrWhiteSpace(nombre))
                {
                    mapa[nombre] = cell.Address.ColumnNumber;
                }
            }

            return mapa;
        }

        private string Obtener(IXLWorksheet sheet, int row, Dictionary<string, int> mapa, string columna)
        {
            if (!mapa.TryGetValue(columna, out int col))
                return string.Empty;

            return sheet.Cell(row, col).GetValue<string>().Trim();
        }

        private DateTime ParseDate(string valor)
        {
            if (DateTime.TryParseExact(valor,
                new[] { "dd-MM-yyyy", "dd/MM/yyyy", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fecha))
            {
                return fecha;
            }

            return DateTime.Parse(valor, CultureInfo.InvariantCulture);
        }

        private decimal ParseDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor == "#e")
                return 0m;

            return decimal.Parse(valor, NumberStyles.Any, Inv);
        }
    }
}