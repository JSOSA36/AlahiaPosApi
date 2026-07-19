using AlahiaPos.Entities.Dto.Fiscal;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Construye XML RFCE (resumen Factura de Consumo &lt; RD$250,000) según XSD RFCE 32.
    /// Preferible armarlo desde el e-CF 32 ya firmado (CodigoSeguridadeCF = primeros 6 del SignatureValue).
    /// </summary>
    public class DgiiRfceBuilder
    {
        public string BuildFromSignedEcf(string xmlEcfFirmado, out string codigoSeguridad)
        {
            if (string.IsNullOrWhiteSpace(xmlEcfFirmado))
                throw new ArgumentException("XML e-CF firmado requerido.", nameof(xmlEcfFirmado));

            codigoSeguridad = AlahiaPos.DataAccess.Seguridad.XmlSigner.ExtractCodigoSeguridad(xmlEcfFirmado)
                              ?? throw new InvalidOperationException("No se pudo extraer SignatureValue del e-CF firmado.");

            var doc = XDocument.Parse(xmlEcfFirmado, LoadOptions.None);
            var enc = doc.Root?.Element("Encabezado")
                       ?? throw new InvalidOperationException("e-CF firmado sin Encabezado.");
            var idDoc = enc.Element("IdDoc") ?? throw new InvalidOperationException("e-CF sin IdDoc.");
            var emisor = enc.Element("Emisor") ?? throw new InvalidOperationException("e-CF sin Emisor.");
            var totales = enc.Element("Totales") ?? throw new InvalidOperationException("e-CF sin Totales.");
            var compradorSrc = enc.Element("Comprador");

            var tipoeCf = (string?)idDoc.Element("TipoeCF") ?? "";
            if (tipoeCf != "32")
                throw new InvalidOperationException($"RFCE solo aplica a tipo 32. Recibido: {tipoeCf}");

            var montoTotal = ParseDec((string?)totales.Element("MontoTotal"));
            if (montoTotal >= 250_000m)
                throw new InvalidOperationException($"MontoTotal {montoTotal} >= 250000; debe ir por canal e-CF individual.");

            var idDocRfce = new XElement("IdDoc",
                new XElement("TipoeCF", 32),
                new XElement("eNCF", ((string?)idDoc.Element("eNCF") ?? "").Trim().ToUpperInvariant()),
                new XElement("TipoIngresos", NormalizeTipoIngresos((string?)idDoc.Element("TipoIngresos"))),
                new XElement("TipoPago", ((string?)idDoc.Element("TipoPago") ?? "1").Trim())
            );

            var tablaFp = idDoc.Element("TablaFormasPago");
            if (tablaFp != null)
            {
                var tabla = new XElement("TablaFormasPago");
                foreach (var f in tablaFp.Elements("FormaDePago").Take(7))
                {
                    var fp = (string?)f.Element("FormaPago");
                    var mp = (string?)f.Element("MontoPago");
                    if (string.IsNullOrWhiteSpace(fp) || string.IsNullOrWhiteSpace(mp)) continue;
                    tabla.Add(new XElement("FormaDePago",
                        new XElement("FormaPago", fp.Trim()),
                        new XElement("MontoPago", FormatMoney(ParseDec(mp)))
                    ));
                }
                if (tabla.HasElements) idDocRfce.Add(tabla);
            }

            var emisorRfce = new XElement("Emisor",
                new XElement("RNCEmisor", DgiiXmlBuilder.NormalizarRnc((string?)emisor.Element("RNCEmisor"))),
                new XElement("RazonSocialEmisor", Trunc((string?)emisor.Element("RazonSocialEmisor") ?? "", 150)),
                new XElement("FechaEmision", ((string?)emisor.Element("FechaEmision") ?? "").Trim())
            );

            // Comprador es obligatorio en XSD aunque todos sus hijos sean opcionales.
            var comprador = new XElement("Comprador");
            if (compradorSrc != null)
            {
                var rncC = (string?)compradorSrc.Element("RNCComprador");
                var idExt = (string?)compradorSrc.Element("IdentificadorExtranjero");
                var razonC = (string?)compradorSrc.Element("RazonSocialComprador");
                if (!string.IsNullOrWhiteSpace(rncC))
                    comprador.Add(new XElement("RNCComprador", DgiiXmlBuilder.NormalizarRnc(rncC)));
                if (!string.IsNullOrWhiteSpace(idExt))
                    comprador.Add(new XElement("IdentificadorExtranjero", Trunc(idExt!, 20)));
                if (!string.IsNullOrWhiteSpace(razonC))
                    comprador.Add(new XElement("RazonSocialComprador", Trunc(razonC!, 150)));
            }

            var totalesRfce = new XElement("Totales");
            CopyMoney(totales, totalesRfce, "MontoGravadoTotal");
            CopyMoney(totales, totalesRfce, "MontoGravadoI1");
            CopyMoney(totales, totalesRfce, "MontoGravadoI2");
            CopyMoney(totales, totalesRfce, "MontoGravadoI3");
            CopyMoney(totales, totalesRfce, "MontoExento");
            CopyMoney(totales, totalesRfce, "TotalITBIS");
            CopyMoney(totales, totalesRfce, "TotalITBIS1");
            CopyMoney(totales, totalesRfce, "TotalITBIS2");
            CopyMoney(totales, totalesRfce, "TotalITBIS3");
            CopyMoneyIfPositive(totales, totalesRfce, "MontoImpuestoAdicional");
            totalesRfce.Add(new XElement("MontoTotal", FormatMoney(montoTotal)));
            CopyMoneyOptional(totales, totalesRfce, "MontoNoFacturable");
            CopyMoneyOptional(totales, totalesRfce, "MontoPeriodo");

            var root = new XElement("RFCE",
                new XElement("Encabezado",
                    new XElement("Version", "1.0"),
                    idDocRfce,
                    emisorRfce,
                    comprador,
                    totalesRfce,
                    new XElement("CodigoSeguridadeCF", codigoSeguridad)
                )
            );

            return SaveXml(root);
        }

        public string Build(FiscalDocumentoElectronico doc, string codigoSeguridadEcf)
        {
            if (doc.Encabezado.TipoEcf != 32)
                throw new InvalidOperationException("RFCE solo aplica a e-CF tipo 32.");

            if (string.IsNullOrWhiteSpace(codigoSeguridadEcf) || codigoSeguridadEcf.Trim().Length < 6)
                throw new ArgumentException("CodigoSeguridadeCF (6 chars del e-CF firmado) es requerido.", nameof(codigoSeguridadEcf));

            var enc = doc.Encabezado;
            var codigo = new string(codigoSeguridadEcf.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (codigo.Length < 6) throw new ArgumentException("CodigoSeguridadeCF inválido.");
            codigo = codigo[..6];

            var idDoc = new XElement("IdDoc",
                new XElement("TipoeCF", 32),
                new XElement("eNCF", enc.Encf.Trim().ToUpperInvariant()),
                new XElement("TipoIngresos", enc.TipoIngreso.ToString("00", CultureInfo.InvariantCulture)),
                new XElement("TipoPago", enc.TipoPago)
            );

            if (doc.FormasPago.Count > 0)
            {
                var tabla = new XElement("TablaFormasPago");
                foreach (var f in doc.FormasPago.Take(7))
                {
                    tabla.Add(new XElement("FormaDePago",
                        new XElement("FormaPago", f.FormaPago),
                        new XElement("MontoPago", FormatMoney(f.Monto))
                    ));
                }
                idDoc.Add(tabla);
            }

            var emisor = new XElement("Emisor",
                new XElement("RNCEmisor", DgiiXmlBuilder.NormalizarRnc(enc.RncEmisor)),
                new XElement("RazonSocialEmisor", Trunc(enc.RazonSocialEmisor, 150)),
                new XElement("FechaEmision", FormatDate(enc.FechaEmision))
            );

            var comprador = new XElement("Comprador");
            if (!string.IsNullOrWhiteSpace(enc.RncComprador))
                comprador.Add(new XElement("RNCComprador", DgiiXmlBuilder.NormalizarRnc(enc.RncComprador!)));
            if (!string.IsNullOrWhiteSpace(enc.RazonSocialComprador))
                comprador.Add(new XElement("RazonSocialComprador", Trunc(enc.RazonSocialComprador!, 150)));

            var totales = new XElement("Totales");
            AddIfPositive(totales, "MontoGravadoTotal", enc.MontoGravadoTotal);
            AddIfPositive(totales, "MontoGravadoI1", enc.MontoGravadoI1);
            AddIfPositive(totales, "MontoGravadoI2", enc.MontoGravadoI2);
            AddIfPositive(totales, "MontoGravadoI3", enc.MontoGravadoI3);
            AddIfPositive(totales, "MontoExento", enc.MontoExento);
            AddIfPositive(totales, "TotalITBIS", enc.TotalItbis);
            AddIfPositive(totales, "TotalITBIS1", enc.TotalItbis1);
            AddIfPositive(totales, "TotalITBIS2", enc.TotalItbis2);
            AddIfPositive(totales, "TotalITBIS3", enc.TotalItbis3);
            totales.Add(new XElement("MontoTotal", FormatMoney(enc.MontoTotal)));

            var root = new XElement("RFCE",
                new XElement("Encabezado",
                    new XElement("Version", "1.0"),
                    idDoc,
                    emisor,
                    comprador,
                    totales,
                    new XElement("CodigoSeguridadeCF", codigo)
                )
            );

            return SaveXml(root);
        }

        public static string NombreArchivo(string rncEmisor, string encf)
            => $"{DgiiXmlBuilder.NormalizarRnc(rncEmisor)}{new string((encf ?? "").Where(char.IsLetterOrDigit).ToArray())}.xml";

        private static string SaveXml(XElement root)
        {
            // No borrar Comprador aunque quede vacío (XSD minOccurs=1).
            root.Descendants()
                .Where(e =>
                    e.Name.LocalName != "Comprador" &&
                    !e.HasElements &&
                    string.IsNullOrEmpty(e.Value) &&
                    !e.HasAttributes)
                .Remove();

            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false,
                Indent = false,
                NewLineHandling = NewLineHandling.None
            };

            using var ms = new MemoryStream();
            using (var writer = XmlWriter.Create(ms, settings))
            {
                new XDocument(new XDeclaration("1.0", "utf-8", null), root).Save(writer);
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static void CopyMoney(XElement src, XElement dst, string name)
        {
            var v = (string?)src.Element(name);
            if (string.IsNullOrWhiteSpace(v)) return;
            var d = ParseDec(v);
            if (d > 0) dst.Add(new XElement(name, FormatMoney(d)));
        }

        private static void CopyMoneyIfPositive(XElement src, XElement dst, string name)
        {
            var v = (string?)src.Element(name);
            if (string.IsNullOrWhiteSpace(v)) return;
            var d = ParseDec(v);
            if (d > 0) dst.Add(new XElement(name, FormatMoney(d)));
        }

        private static void CopyMoneyOptional(XElement src, XElement dst, string name)
        {
            var v = (string?)src.Element(name);
            if (string.IsNullOrWhiteSpace(v)) return;
            dst.Add(new XElement(name, FormatMoney(ParseDec(v))));
        }

        private static void AddIfPositive(XElement parent, string name, decimal value)
        {
            if (value > 0) parent.Add(new XElement(name, FormatMoney(value)));
        }

        private static string NormalizeTipoIngresos(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "01";
            v = v.Trim();
            if (int.TryParse(v, out var n)) return n.ToString("00", CultureInfo.InvariantCulture);
            return v.PadLeft(2, '0');
        }

        private static decimal ParseDec(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return decimal.Parse(s.Trim(), CultureInfo.InvariantCulture);
        }

        private static string FormatDate(DateTime d) => d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        private static string FormatMoney(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);
        private static string Trunc(string s, int max)
        {
            s = (s ?? "").Trim();
            return s.Length <= max ? s : s[..max];
        }
    }
}
