using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

const int IdEmpresa = 60;
const string RncDoctora = "133659115";
const string Razon = "CENTRO ODONTOLOGICO DRA SENA 1723 SRL";
const string Comercial = "CENTRO ODONTOLOGICO DRA SENA 1723";
const string Direccion = "Santo Domingo";
const string Telefono = "849-255-6007";
const string EncfNuevo = "E310000000081";
const string Ambiente = "testecf";
const string Cs = @"Server=144.126.143.154\SQLEXPRESS,1433;Database=AlahiaPos_Dev;User Id=sa;Password=JoelAriel8787;Encrypt=False";
var outXml = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CerteCF-SUBIR-XML");
Directory.CreateDirectory(outXml);

var cert = LoadCert();
Console.WriteLine("CERT " + cert.Subject + " exp " + cert.NotAfter.ToString("yyyy-MM-dd"));
var token = await ObtenerTokenAsync(cert);
Console.WriteLine("TOKEN OK rnc=" + RncDoctora);

var nowDt = DateTime.UtcNow.AddHours(-4);
var now = nowDt.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
var hoy = nowDt.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
var vence = "31-12-2028";

var limpio = BuildE31DesdeCero(EncfNuevo, hoy, vence, now);
var firmado = SignXml(limpio, cert);
var dest = Path.Combine(outXml, RncDoctora + EncfNuevo + ".xml");
File.WriteAllText(dest, firmado, new UTF8Encoding(false));
var codigo = ExtractCodigo(firmado) ?? "";
var qrEmit = QrDesdeXmlFirmado(firmado, codigo);
Console.WriteLine("XML NUEVO (sin plantilla) " + dest);
Console.WriteLine("eNCF=" + EncfNuevo + " razon=" + Razon + " codigo=" + codigo);

var resp = await EnviarAsync(token, firmado, RncDoctora + EncfNuevo + ".xml", rfce: false);
var qrDgii = NullIfEmpty(resp.Qr) ?? qrEmit;
Console.WriteLine($"SEND {resp.Status} ok={resp.Ok} estado={resp.Estado} track={resp.TrackId}");
Console.WriteLine("QR_EMIT " + qrDgii);
Console.WriteLine("BODY " + Cortar(resp.Mensaje, 400));
if (string.IsNullOrWhiteSpace(resp.TrackId))
{
    Console.WriteLine("SIN TRACK — DGII no devolvió envío. No generar PDF.");
    return 1;
}

Console.WriteLine("Esperando 8s para consulta TrackId...");
await Task.Delay(8000);
var cons = await ConsultarAsync(token, resp.TrackId!);
if (!string.IsNullOrWhiteSpace(cons.Qr))
    qrDgii = cons.Qr!;
Console.WriteLine($"CONSULTA {EncfNuevo} codigo={codigo} estado={cons.Estado} {Cortar(cons.Mensaje, 300)}");
Console.WriteLine("QR_DGII " + qrDgii);
var aceptado = string.Equals(cons.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase)
    || string.Equals(cons.Estado, "AceptadoCondicional", StringComparison.OrdinalIgnoreCase);
if (!aceptado)
{
    Console.WriteLine("NO ACEPTADO — no hay e-CF guardado en DGII. No reciclar QR viejo.");
    return 1;
}

File.WriteAllText(Path.Combine(outXml, "qr-emit.txt"), qrDgii, new UTF8Encoding(false));
Console.WriteLine("ACEPTADO — RI debe usar este QR del emit.");
return 0;

static string BuildE31DesdeCero(string encf, string fechaEmision, string fechaVence, string fechaFirma)
{
    var xml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<ECF>
  <Encabezado>
    <Version>1.0</Version>
    <IdDoc>
      <TipoeCF>31</TipoeCF>
      <eNCF>{encf}</eNCF>
      <FechaVencimientoSecuencia>{fechaVence}</FechaVencimientoSecuencia>
      <IndicadorMontoGravado>0</IndicadorMontoGravado>
      <TipoIngresos>01</TipoIngresos>
      <TipoPago>1</TipoPago>
      <TablaFormasPago>
        <FormaDePago>
          <FormaPago>1</FormaPago>
          <MontoPago>118.00</MontoPago>
        </FormaDePago>
      </TablaFormasPago>
    </IdDoc>
    <Emisor>
      <RNCEmisor>{RncDoctora}</RNCEmisor>
      <RazonSocialEmisor>{Razon}</RazonSocialEmisor>
      <NombreComercial>{Comercial}</NombreComercial>
      <DireccionEmisor>{Direccion}</DireccionEmisor>
      <TablaTelefonoEmisor>
        <TelefonoEmisor>{Telefono}</TelefonoEmisor>
      </TablaTelefonoEmisor>
      <FechaEmision>{fechaEmision}</FechaEmision>
    </Emisor>
    <Comprador>
      <RNCComprador>131880738</RNCComprador>
      <RazonSocialComprador>CLIENTE DEMO SA</RazonSocialComprador>
    </Comprador>
    <Totales>
      <MontoGravadoTotal>100.00</MontoGravadoTotal>
      <MontoGravadoI1>100.00</MontoGravadoI1>
      <ITBIS1>18</ITBIS1>
      <TotalITBIS>18.00</TotalITBIS>
      <TotalITBIS1>18.00</TotalITBIS1>
      <MontoTotal>118.00</MontoTotal>
    </Totales>
  </Encabezado>
  <DetallesItems>
    <Item>
      <NumeroLinea>1</NumeroLinea>
      <IndicadorFacturacion>1</IndicadorFacturacion>
      <NombreItem>Consulta odontologica</NombreItem>
      <IndicadorBienoServicio>2</IndicadorBienoServicio>
      <CantidadItem>1.00</CantidadItem>
      <UnidadMedida>43</UnidadMedida>
      <PrecioUnitarioItem>100.00</PrecioUnitarioItem>
      <MontoItem>100.00</MontoItem>
    </Item>
  </DetallesItems>
  <FechaHoraFirma>{fechaFirma}</FechaHoraFirma>
</ECF>";
    return xml;
}

static X509Certificate2 LoadCert()
{
    using var cn = new SqlConnection(Cs);
    cn.Open();
    using var cmd = new SqlCommand(@"
SELECT TOP 1 ArchivoBytes, PasswordEncriptado, RutaArchivo
FROM dbo.CertificadoDigital
WHERE IdEmpresa=@e AND Activo=1
ORDER BY FechaCreacion DESC", cn);
    cmd.Parameters.AddWithValue("@e", IdEmpresa);
    using var rd = cmd.ExecuteReader();
    if (!rd.Read()) throw new InvalidOperationException("No hay certificado activo empresa 60.");
    var bytes = rd.IsDBNull(0) ? null : (byte[])rd[0];
    var pass = rd.IsDBNull(1) ? "" : rd.GetString(1);
    var ruta = rd.IsDBNull(2) ? null : rd.GetString(2);
    const X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable;
    if (bytes is { Length: > 0 }) return new X509Certificate2(bytes, pass, flags);
    if (!string.IsNullOrWhiteSpace(ruta) && File.Exists(ruta)) return new X509Certificate2(ruta, pass, flags);
    throw new InvalidOperationException("Certificado sin .p12.");
}

static string QuitarFirma(string xml)
{
    var doc = new XmlDocument { PreserveWhitespace = false };
    doc.LoadXml(xml);
    var nsmgr = new XmlNamespaceManager(doc.NameTable);
    nsmgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
    var nodos = doc.SelectNodes("//ds:Signature | //*[local-name()='Signature']", nsmgr);
    if (nodos != null)
    {
        foreach (XmlNode n in nodos.Cast<XmlNode>().ToList())
            n.ParentNode?.RemoveChild(n);
    }
    return doc.DocumentElement!.OuterXml;
}

static string ReemplazarTag(string xml, string localName, string valor, bool obligatorio)
{
    var doc = new XmlDocument { PreserveWhitespace = false };
    doc.LoadXml(xml);
    var nodes = doc.SelectNodes("//*[local-name()='" + localName + "']");
    if (nodes == null || nodes.Count == 0)
    {
        if (obligatorio) throw new InvalidOperationException("No está <" + localName + ">");
        return xml;
    }
    foreach (XmlNode n in nodes)
        n.InnerText = valor;
    return doc.DocumentElement!.OuterXml;
}

static string AsegurarFechaHoraFirma(string xml, string valor)
{
    var doc = new XmlDocument { PreserveWhitespace = false };
    doc.LoadXml(xml);
    var node = doc.SelectSingleNode("//*[local-name()='FechaHoraFirma']");
    if (node != null)
        node.InnerText = valor;
    else
    {
        var el = doc.CreateElement("FechaHoraFirma");
        el.InnerText = valor;
        doc.DocumentElement!.AppendChild(el);
    }
    return doc.DocumentElement!.OuterXml;
}

static string QrDesdeXmlFirmado(string xmlFirmado, string codigo)
{
    var rncE = Digitos(ValorTag(xmlFirmado, "RNCEmisor"));
    var rncC = Digitos(ValorTag(xmlFirmado, "RNCComprador"));
    var encf = ValorTag(xmlFirmado, "eNCF") ?? "";
    var fechaEm = ValorTag(xmlFirmado, "FechaEmision") ?? "";
    var montoRaw = ValorTag(xmlFirmado, "MontoTotal") ?? "0";
    var monto = decimal.TryParse(montoRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var m)
        ? m.ToString("0.00", CultureInfo.InvariantCulture)
        : montoRaw;
    var fechaFirma = ValorTag(xmlFirmado, "FechaHoraFirma") ?? "";
    var parts = new List<string>
    {
        "RncEmisor=" + Uri.EscapeDataString(rncE)
    };
    if (!string.IsNullOrEmpty(rncC))
        parts.Add("RncComprador=" + Uri.EscapeDataString(rncC));
    parts.Add("ENCF=" + Uri.EscapeDataString(encf));
    parts.Add("FechaEmision=" + Uri.EscapeDataString(fechaEm));
    parts.Add("MontoTotal=" + Uri.EscapeDataString(monto));
    parts.Add("FechaFirma=" + fechaFirma.Replace(" ", "%20"));
    parts.Add("CodigoSeguridad=" + Uri.EscapeDataString(codigo));
    return $"https://ecf.dgii.gov.do/{Ambiente}/ConsultaTimbre?" + string.Join("&", parts);
}

static string Digitos(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

static string? ValorTag(string xml, string localName)
{
    var doc = new XmlDocument();
    doc.LoadXml(xml);
    return doc.SelectSingleNode("//*[local-name()='" + localName + "']")?.InnerText?.Trim();
}

static string SignXml(string xml, X509Certificate2 cert)
{
    var doc = new XmlDocument { PreserveWhitespace = false };
    doc.LoadXml(xml);
    var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
    signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    var reference = new Reference { Uri = "" };
    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
    reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
    signedXml.AddReference(reference);
    var keyInfo = new KeyInfo();
    keyInfo.AddClause(new KeyInfoX509Data(cert));
    signedXml.KeyInfo = keyInfo;
    signedXml.ComputeSignature();
    doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));
    return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + doc.DocumentElement!.OuterXml;
}

static string? ExtractCodigo(string xmlFirmado)
{
    var doc = new XmlDocument();
    doc.LoadXml(xmlFirmado);
    var nsmgr = new XmlNamespaceManager(doc.NameTable);
    nsmgr.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
    var node = doc.SelectSingleNode("//ds:SignatureValue", nsmgr)
               ?? doc.SelectSingleNode("//*[local-name()='SignatureValue']");
    var raw = node?.InnerText;
    if (string.IsNullOrEmpty(raw)) return null;
    var val = new string(raw.Where(c => !char.IsWhiteSpace(c)).ToArray());
    return val.Length < 6 ? val : val[..6];
}

static async Task<string> ObtenerTokenAsync(X509Certificate2 cert)
{
    using var http = NewHttp();
    var semillaUrl = $"https://ecf.dgii.gov.do/{Ambiente}/autenticacion/api/autenticacion/semilla";
    using var req = new HttpRequestMessage(HttpMethod.Get, semillaUrl);
    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
    using var res = await http.SendAsync(req);
    var body = await res.Content.ReadAsStringAsync();
    if (!res.IsSuccessStatusCode)
        throw new InvalidOperationException($"semilla {(int)res.StatusCode}: {body}");
    var firmada = SignXml(StripXmlDecl(body), cert);
    var validarUrl = $"https://ecf.dgii.gov.do/{Ambiente}/autenticacion/api/autenticacion/validarsemilla";
    using var form = new MultipartFormDataContent();
    var file = new ByteArrayContent(Encoding.UTF8.GetBytes(firmada));
    file.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
    form.Add(file, "xml", "semilla_firmada.xml");
    using var vreq = new HttpRequestMessage(HttpMethod.Post, validarUrl) { Content = form };
    vreq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    using var vres = await http.SendAsync(vreq);
    var vbody = await vres.Content.ReadAsStringAsync();
    if (!vres.IsSuccessStatusCode)
        throw new InvalidOperationException($"validar {(int)vres.StatusCode}: {vbody}");
    return ParseToken(vbody);
}

static string StripXmlDecl(string xml)
{
    xml = xml.Trim();
    if (xml.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
    {
        var i = xml.IndexOf("?>", StringComparison.Ordinal);
        if (i >= 0) xml = xml[(i + 2)..].Trim();
    }
    return xml;
}

static string ParseToken(string resp)
{
    try
    {
        using var doc = JsonDocument.Parse(resp);
        var token = doc.RootElement.TryGetProperty("token", out var t) ? t.GetString()
            : doc.RootElement.TryGetProperty("Token", out t) ? t.GetString() : null;
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("sin token json: " + resp);
        return token!;
    }
    catch (JsonException)
    {
        var x = XDocument.Parse(resp);
        var token = x.Descendants().FirstOrDefault(n => n.Name.LocalName.Equals("token", StringComparison.OrdinalIgnoreCase))?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("sin token xml: " + resp);
        return token!;
    }
}

static async Task<SendResp> EnviarAsync(string token, string xmlFirmado, string nombre, bool rfce)
{
    using var http = NewHttp();
    var url = rfce
        ? $"https://fc.dgii.gov.do/{Ambiente}/recepcionfc/api/recepcion/ecf"
        : $"https://ecf.dgii.gov.do/{Ambiente}/recepcion/api/facturaselectronicas";
    using var form = new MultipartFormDataContent();
    var file = new ByteArrayContent(Encoding.UTF8.GetBytes(xmlFirmado));
    file.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
    form.Add(file, "xml", nombre);
    using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    using var res = await http.SendAsync(req);
    var body = await res.Content.ReadAsStringAsync();
    var headers = string.Join("; ", res.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value))
        .Concat(res.Content.Headers.Select(h => h.Key + "=" + string.Join(",", h.Value))));
    var parsed = ParseSend(res.IsSuccessStatusCode, (int)res.StatusCode, body);
    if (string.IsNullOrWhiteSpace(body))
        parsed.Mensaje = $"(vacio ct={res.Content.Headers.ContentType} hdr={headers})";
    else
        parsed.Mensaje = (parsed.Mensaje ?? body) + " | RAW=" + Cortar(body, 300);
    return parsed;
}

static async Task<SendResp> ConsultarAsync(string token, string trackId)
{
    using var http = NewHttp();
    var url = $"https://ecf.dgii.gov.do/{Ambiente}/consultaresultado/api/consultas/estado?TrackId=" + Uri.EscapeDataString(trackId);
    using var req = new HttpRequestMessage(HttpMethod.Get, url);
    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    using var res = await http.SendAsync(req);
    var body = await res.Content.ReadAsStringAsync();
    return ParseSend(res.IsSuccessStatusCode, (int)res.StatusCode, body);
}

static string BuildRfce(string xmlEcfFirmado)
{
    var x = XDocument.Parse(xmlEcfFirmado);
    string? T(XContainer? c, string n) => c?.Descendants().FirstOrDefault(e => e.Name.LocalName == n)?.Value?.Trim();
    var enc = x.Root?.Element("Encabezado") ?? throw new InvalidOperationException("RFCE sin Encabezado");
    var idDoc = enc.Element("IdDoc")!;
    var emisor = enc.Element("Emisor")!;
    var tot = enc.Element("Totales")!;
    var codigo = ExtractCodigo(xmlEcfFirmado) ?? throw new InvalidOperationException("RFCE sin codigo");
    var id = new XElement("IdDoc",
        new XElement("TipoeCF", "32"),
        new XElement("eNCF", T(idDoc, "eNCF")),
        new XElement("TipoIngresos", (T(idDoc, "TipoIngresos") ?? "01").PadLeft(2, '0')),
        new XElement("TipoPago", T(idDoc, "TipoPago") ?? "1"));
    var em = new XElement("Emisor",
        new XElement("RNCEmisor", T(emisor, "RNCEmisor")),
        new XElement("RazonSocialEmisor", T(emisor, "RazonSocialEmisor")),
        new XElement("FechaEmision", T(emisor, "FechaEmision")));
    var compSrc = enc.Element("Comprador");
    var comp = new XElement("Comprador");
    if (!string.IsNullOrWhiteSpace(T(compSrc, "RNCComprador")))
        comp.Add(new XElement("RNCComprador", T(compSrc, "RNCComprador")));
    if (!string.IsNullOrWhiteSpace(T(compSrc, "RazonSocialComprador")))
        comp.Add(new XElement("RazonSocialComprador", T(compSrc, "RazonSocialComprador")));
    var totOut = new XElement("Totales");
    foreach (var n in new[] { "MontoGravadoTotal", "MontoGravadoI1", "MontoGravadoI2", "MontoGravadoI3", "MontoExento", "TotalITBIS", "TotalITBIS1", "TotalITBIS2", "TotalITBIS3", "MontoTotal", "MontoNoFacturable", "MontoPeriodo" })
    {
        var v = T(tot, n);
        if (!string.IsNullOrWhiteSpace(v)) totOut.Add(new XElement(n, v));
    }
    var root = new XElement("RFCE",
        new XElement("Encabezado",
            new XElement("Version", "1.0"),
            id, em, comp, totOut,
            new XElement("CodigoSeguridadeCF", codigo)));
    return root.ToString(SaveOptions.DisableFormatting);
}

static SendResp ParseSend(bool ok, int status, string body)
{
    var r = new SendResp { Ok = ok, Status = status, Mensaje = body };
    try
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string? G(string a, string b) =>
            root.TryGetProperty(a, out var p) ? p.ToString() : root.TryGetProperty(b, out p) ? p.ToString() : null;
        r.Estado = G("estado", "Estado");
        r.TrackId = G("trackId", "TrackId");
        r.Qr = NullIfEmpty(G("qr", "Qr")) ?? NullIfEmpty(G("urlQR", "UrlQR"));
        r.Mensaje = NullIfEmpty(G("mensaje", "Mensaje")) ?? NullIfEmpty(G("error", "Error")) ?? body;
        if (root.TryGetProperty("secuenciaUtilizada", out var su) || root.TryGetProperty("SecuenciaUtilizada", out su))
            r.SecuenciaUtilizada = su.ValueKind == JsonValueKind.True || (su.ValueKind == JsonValueKind.String && su.GetString() == "true");
        if (root.TryGetProperty("mensajes", out var msgs) || root.TryGetProperty("Mensajes", out msgs))
        {
            if (msgs.ValueKind == JsonValueKind.Array)
            {
                var parts = new List<string>();
                foreach (var m in msgs.EnumerateArray())
                    parts.Add(m.TryGetProperty("valor", out var v) ? v.ToString() : m.ToString());
                if (parts.Count > 0) r.Mensaje = string.Join(" | ", parts);
            }
        }
    }
    catch
    {
        try
        {
            var x = XDocument.Parse(body);
            r.Estado ??= x.Descendants().FirstOrDefault(n => n.Name.LocalName.Equals("estado", StringComparison.OrdinalIgnoreCase))?.Value;
            r.TrackId ??= x.Descendants().FirstOrDefault(n => n.Name.LocalName.Equals("trackId", StringComparison.OrdinalIgnoreCase))?.Value;
            r.Mensaje = NullIfEmpty(x.Descendants().FirstOrDefault(n => n.Name.LocalName.Equals("mensaje", StringComparison.OrdinalIgnoreCase))?.Value) ?? body;
        }
        catch { /* raw */ }
    }
    return r;
}

static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

static bool EsAceptado(SendResp r)
{
    var t = ((r.Estado ?? "") + " " + (r.Mensaje ?? "")).ToLowerInvariant();
    if (t.Contains("rechazado")) return false;
    return t.Contains("aceptado");
}

static HttpClient NewHttp()
{
    var http = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("AlahiaERP-CerteCF/1.0");
    return http;
}

static string Cortar(string? s, int n)
{
    s ??= "";
    s = s.Replace("\r", " ").Replace("\n", " ");
    return s.Length <= n ? s : s[..n];
}

sealed class SendResp
{
    public bool Ok { get; set; }
    public int Status { get; set; }
    public string? Estado { get; set; }
    public string? TrackId { get; set; }
    public string? Qr { get; set; }
    public string? Mensaje { get; set; }
    public bool? SecuenciaUtilizada { get; set; }
}
