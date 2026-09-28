using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Fiscal;

var jsonOpts = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var emisor = new CertecfRiEmisor
{
    Rnc = CertecfArtefactos.RncDraSena,
    RazonSocial = CertecfArtefactos.RazonSocialDraSena,
    NombreComercial = CertecfArtefactos.NombreComercialDraSena,
    Direccion = "Santo Domingo",
    Telefono = "8492556007"
};

var src = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Documents", "GitHub", "AlahiaPosApi", "artifacts", "gold-testecf");
if (!Directory.Exists(src))
{
    Console.WriteLine("Falta " + src);
    return 1;
}

var permitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "E310000000010",
    "E320000000016", "E320000000017", "E320000000021",
    "E330000000002",
    "E340000000019",
    "E410000000011",
    "E430000000012",
    "E440000000011",
    "E450000000011",
    "E460000000012",
    "E470000000011"
};

var docs = new List<(FiscalDocumentoElectronico Doc, string Respuesta)>();
foreach (var file in Directory.GetFiles(src, "wire_E*.json"))
{
    var doc = JsonSerializer.Deserialize<FiscalDocumentoElectronico>(File.ReadAllText(file), jsonOpts);
    if (doc?.Encabezado == null || !permitidos.Contains(doc.Encabezado.Encf)) continue;
    doc.AmbienteDgii = "certecf";
    CertecfArtefactos.AplicarIdentidadEmisorReal(
        doc, CertecfArtefactos.RazonSocialDraSena, CertecfArtefactos.NombreComercialDraSena);
    CertecfArtefactos.AsegurarFechaVencimientoSecuenciaCertecf(doc);
    docs.Add((doc, ""));
}

var slots = new (string Clave, int Tipo, Func<FiscalDocumentoElectronico, bool> Match)[]
{
    ("tipo-31", 31, d => d.Encabezado.TipoEcf == 31 && d.Encabezado.Encf != "E310000000009"),
    ("tipo-32-250mil", 32, d => d.Encabezado.TipoEcf == 32 && d.Encabezado.MontoTotal >= 250000m),
    ("tipo-33", 33, d => d.Encabezado.TipoEcf == 33),
    ("tipo-34", 34, d => d.Encabezado.TipoEcf == 34),
    ("tipo-41", 41, d => d.Encabezado.TipoEcf == 41),
    ("tipo-43", 43, d => d.Encabezado.TipoEcf == 43),
    ("tipo-44", 44, d => d.Encabezado.TipoEcf == 44),
    ("tipo-45", 45, d => d.Encabezado.TipoEcf == 45),
    ("tipo-46", 46, d => d.Encabezado.TipoEcf == 46),
    ("tipo-47", 47, d => d.Encabezado.TipoEcf == 47),
    ("tipo-32-consumo", 32, d => d.Encabezado.TipoEcf == 32 && d.Encabezado.MontoTotal < 250000m),
};

var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
var outDir = Path.Combine(desktop, "CerteCF-SUBIR-RI");
Directory.CreateDirectory(outDir);
foreach (var old in Directory.GetFiles(outDir, "??-RI-*.pdf"))
    File.Delete(old);

var ok = 0;
for (var i = 0; i < slots.Length; i++)
{
    var def = slots[i];
    var hit = docs.Where(x => def.Match(x.Doc)).OrderBy(x => x.Doc.Encabezado.Encf).FirstOrDefault();
    if (hit.Doc == null)
        hit = docs.Where(x => x.Doc.Encabezado.TipoEcf == def.Tipo && x.Doc.Encabezado.Encf != "E310000000009")
            .OrderBy(x => x.Doc.Encabezado.Encf)
            .FirstOrDefault();
    if (hit.Doc == null)
    {
        Console.WriteLine($"FALTA {def.Clave}");
        continue;
    }

    var doc = hit.Doc;
    var xml = CertecfArtefactos.BuscarXmlFirmado(doc.Encabezado.Encf, doc.Encabezado.RncEmisor);
    AplicarFechasXml(doc, xml);
    var qr = CertecfArtefactos.ExtraerQrDeRespuesta(hit.Respuesta);
    if (!EsHttp(qr))
        qr = QrDesdeXml(doc, xml, hit.Respuesta);
    if (!EsHttp(qr))
        Console.WriteLine($"SIN QR de envío {def.Clave} {doc.Encabezado.Encf} — PDF igual, para revisar visual.");

    var (codigo, fechaFirma) = CertecfArtefactos.TimbreDesdeQr(qr);
    if (!string.IsNullOrWhiteSpace(xml))
    {
        codigo ??= XmlSigner.ExtractCodigoSeguridad(xml);
        var fh = EcfDgiiFecha.ExtraerFechaHoraFirmaXml(xml);
        if (fh.HasValue)
            fechaFirma ??= fh.Value.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }

    var pdf = CertecfRiPdf.Crear(emisor, doc, qr, codigo, fechaFirma);
    var name = $"{i + 1:00}-RI-{def.Clave}-{doc.Encabezado.Encf}.pdf";
    File.WriteAllBytes(Path.Combine(outDir, name), pdf);
    Console.WriteLine($"OK {name} {pdf.Length} {qr}");
    ok++;
}

Console.WriteLine($"{ok}/11 en {outDir}");
return ok == 11 ? 0 : 2;

static bool EsHttp(string? url)
    => !string.IsNullOrWhiteSpace(url)
       && url.StartsWith("http", StringComparison.OrdinalIgnoreCase);

static void AplicarFechasXml(FiscalDocumentoElectronico doc, string? xml)
{
    if (string.IsNullOrWhiteSpace(xml) || doc.Encabezado == null) return;
    var fe = ValorXml(xml, "FechaEmision");
    if (EcfDgiiFecha.Parse(fe) is DateTime f) doc.Encabezado.FechaEmision = f;
    var monto = ValorXml(xml, "MontoTotal");
    if (decimal.TryParse(monto, NumberStyles.Any, CultureInfo.InvariantCulture, out var m))
        doc.Encabezado.MontoTotal = m;
}

static string? QrDesdeXml(FiscalDocumentoElectronico doc, string? xml, string respuesta)
{
    if (string.IsNullOrWhiteSpace(xml)) return null;
    var codigo = XmlSigner.ExtractCodigoSeguridad(xml);
    var fechaFirma = EcfDgiiFecha.ExtraerFechaHoraFirmaXml(xml);
    if (string.IsNullOrWhiteSpace(codigo) || !fechaFirma.HasValue) return null;
    var fe = EcfDgiiFecha.Parse(ValorXml(xml, "FechaEmision")) ?? doc.Encabezado.FechaEmision;
    var montoTxt = ValorXml(xml, "MontoTotal");
    var monto = decimal.TryParse(montoTxt, NumberStyles.Any, CultureInfo.InvariantCulture, out var m)
        ? m
        : doc.Encabezado.MontoTotal;
    var rncComprador = ValorXml(xml, "RNCComprador") ?? doc.Encabezado.RncComprador;
    var encf = ValorXml(xml, "eNCF") ?? doc.Encabezado.Encf;
    var rncEmisor = ValorXml(xml, "RNCEmisor") ?? doc.Encabezado.RncEmisor;
    var ambiente = EcfConsultaTimbreUrl.ExtraerAmbienteDeUrl(respuesta) ?? "certecf";
    return EcfConsultaTimbreUrl.Build(
        ambiente,
        doc.Encabezado.TipoEcf,
        rncEmisor ?? "",
        rncComprador,
        encf ?? "",
        fe,
        monto,
        fechaFirma.Value,
        codigo);
}

static string? ValorXml(string xml, string localName)
{
    try
    {
        var x = XDocument.Parse(xml);
        var raw = x.Descendants().FirstOrDefault(n => n.Name.LocalName == localName)?.Value;
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }
    catch
    {
        return null;
    }
}
