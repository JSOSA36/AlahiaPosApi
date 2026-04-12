using Alahia.eCF.Api.Dto;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using OfficeOpenXml;

namespace AlahiaPos.DataAccess.Servicios;

public class ExcelMapperService : IExcelMapperService
{
    public async Task<List<EcfDto>> Mapear(Stream excelStream, int tipoeCF)
    {
        ExcelPackage.License.SetNonCommercialPersonal("Joel Sosa");

        using var package = new ExcelPackage(excelStream);
        var sheet = package.Workbook.Worksheets[0];

        var headers = new Dictionary<string, int>();

        // 🔥 Leer encabezados
        for (int col = 1; col <= sheet.Dimension.End.Column; col++)
        {
            var header = sheet.Cells[1, col].Text;
            if (!string.IsNullOrEmpty(header))
                headers[header] = col;
        }

        string Get(int row, string name) =>
            headers.ContainsKey(name) ? sheet.Cells[row, headers[name]].Text : "";

        decimal GetDecimal(int row, string name) =>
            decimal.TryParse(Get(row, name), out var val) ? val : 0;

        int GetInt(int row, string name) =>
            int.TryParse(Get(row, name), out var val) ? val : 0;

        var lista = new List<EcfDto>();

        for (int row = 2; row <= sheet.Dimension.End.Row; row++)
        {
            var tipo = GetInt(row, "TipoeCF");

            if (tipo != tipoeCF)
                continue;

            var dto = new EcfDto
            {
                Encabezado = new EncabezadoDto
                {
                    Version = Get(row, "Version"),

                    IdDoc = new IdDocDto
                    {
                        TipoeCF = tipo,
                        eNCF = Get(row, "ENCF"),
                        FechaVencimientoSecuencia = "31-12-2028",
                        IndicadorMontoGravado = GetInt(row, "IndicadorMontoGravado"),
                        TipoIngresos = Get(row, "TipoIngresos"),
                        TipoPago = GetInt(row, "TipoPago")
                    },

                    Emisor = new EmisorDto
                    {
                        RNCEmisor = Get(row, "RNCEmisor"),
                        RazonSocialEmisor = Get(row, "RazonSocialEmisor"),
                        NombreComercial = Get(row, "NombreComercial"),
                        DireccionEmisor = Get(row, "DireccionEmisor"),
                        Municipio = Get(row, "Municipio"),
                        Provincia = Get(row, "Provincia"),

                        // 🔥 POSICIÓN EXACTA SEGÚN DGII
                        TablaTelefonoEmisor = new TablaTelefonoEmisorDto
                        {
                            TelefonoEmisor = new List<string>
                            {
                                Get(row, "TelefonoEmisor[1]"),
                                Get(row, "TelefonoEmisor[2]")
                            }
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList()
                        },

                        CorreoEmisor = Get(row, "CorreoEmisor"),
                        WebSite = Get(row, "WebSite"),
                        CodigoVendedor = Get(row, "CodigoVendedor"),
                        NumeroFacturaInterna = Get(row, "NumeroFacturaInterna"),
                        NumeroPedidoInterno = Get(row, "NumeroPedidoInterno"),
                        ZonaVenta = Get(row, "ZonaVenta"),
                        FechaEmision = Get(row, "FechaEmision")
                    },

                    Comprador = new CompradorDto
                    {
                        RNCComprador = Get(row, "RNCComprador"),
                        RazonSocialComprador = Get(row, "RazonSocialComprador"),
                        ContactoComprador = Get(row, "ContactoComprador"),
                        CorreoComprador = Get(row, "CorreoComprador"),
                        DireccionComprador = Get(row, "DireccionComprador"),
                        MunicipioComprador = Get(row, "MunicipioComprador"),
                        ProvinciaComprador = Get(row, "ProvinciaComprador"),
                        FechaEntrega = Get(row, "FechaEntrega"),
                        FechaOrdenCompra = Get(row, "FechaOrdenCompra"),
                        NumeroOrdenCompra = Get(row, "NumeroOrdenCompra"),
                        CodigoInternoComprador = Get(row, "CodigoInternoComprador")
                    },

                    Totales = new TotalesDto
                    {
                        MontoGravadoTotal = GetDecimal(row, "MontoGravadoTotal"),
                        MontoGravadoI1 = GetDecimal(row, "MontoGravadoI1"),
                        ITBIS1 = 18,
                        TotalITBIS = GetDecimal(row, "TotalITBIS"),
                        TotalITBIS1 = GetDecimal(row, "TotalITBIS1"),
                        MontoTotal = GetDecimal(row, "MontoTotal"),
                        MontoPeriodo = GetDecimal(row, "MontoPeriodo"),
                        ValorPagar = GetDecimal(row, "ValorPagar")
                    }
                },

                DetallesItems = new List<ItemDto>
                {
                    new ItemDto
                    {
                        

                        NumeroLinea = 1,
                        IndicadorFacturacion = 1,
                        NombreItem = Get(row, "NombreItem[1]"),
                        IndicadorBienoServicio = GetInt(row, "IndicadorBienoServicio[1]"),
                        CantidadItem = GetDecimal(row, "CantidadItem[1]"),
                        UnidadMedida = GetInt(row, "UnidadMedida[1]"),
                        PrecioUnitarioItem = GetDecimal(row, "PrecioUnitarioItem[1]"),
                        MontoItem = GetDecimal(row, "MontoItem[1]")
                    }
                },

                FechaHoraFirma = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
            };

            lista.Add(dto);
        }

        return lista;
    }
}