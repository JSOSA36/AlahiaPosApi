using System.Globalization;
using System.Text.Json;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;

var jsonPath = args.Length > 0 ? args[0] : @"C:\Users\USUARIO\AppData\Local\Temp\e34-216.raw";
var outPath = args.Length > 1
    ? args[1]
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "CerteCF-SUBIR-RI",
        "04-RI-tipo-34-E340000000021.pdf");

var qr = "https://ecf.dgii.gov.do/certecf/ConsultaTimbre?RncEmisor=133659115&RncComprador=131880681&ENCF=E340000000021&FechaEmision=02-12-2018&MontoTotal=0.00&FechaFirma=18-09-2026%2008:53:49&CodigoSeguridad=V9wO8W";

var opts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(File.ReadAllText(jsonPath), opts)
    ?? throw new InvalidOperationException("No se pudo leer el payload.");
// XML firmado aceptado: FechaEmision 02-12-2018, MontoTotal 0.00, MontoNoFacturable 1.00.
doc.Encabezado.FechaEmision = new DateTime(2018, 12, 2);
doc.Encabezado.MontoTotal = 0m;
doc.Encabezado.TotalItbis = 0m;
doc.Encabezado.MontoNoFacturable = 1m;

var emisor = new CertecfRiEmisor
{
    Rnc = CertecfArtefactos.RncDraSena,
    RazonSocial = CertecfArtefactos.RazonSocialDraSena,
    NombreComercial = CertecfArtefactos.NombreComercialDraSena,
    Direccion = "Santo Domingo",
    Telefono = "8492556007"
};

var (codigo, fechaFirma) = CertecfArtefactos.TimbreDesdeQr(qr);
var pdf = CertecfRiPdf.Crear(emisor, doc, qr, codigo, fechaFirma);
Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
File.WriteAllBytes(outPath, pdf);
Console.WriteLine($"{outPath} {pdf.Length} total={doc.Encabezado.MontoTotal.ToString("0.00", CultureInfo.InvariantCulture)} fecha={doc.Encabezado.FechaEmision:dd-MM-yyyy}");
