using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public static class CertecfReceptorUrls
    {
        public static string Digits(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        public static string Base(string? publicBase)
        {
            var b = (publicBase ?? "").Trim().TrimEnd('/');
            return string.IsNullOrWhiteSpace(b) ? "https://ecf.alahiapos.com" : b;
        }

        /// <summary>
        /// URL que se declara en CerteCF. Nunca localhost: DGII no puede pegarle.
        /// </summary>
        public static string PublicBase(string? publicBaseUrl, string? localBaseUrl)
        {
            var pub = (publicBaseUrl ?? "").Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(pub) && !EsLocal(pub))
                return pub;

            var local = (localBaseUrl ?? "").Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(local) && !EsLocal(local))
                return local;

            return "https://ecf.alahiapos.com";
        }

        public static bool EsLocal(string? url)
        {
            var u = (url ?? "").ToLowerInvariant();
            return u.Contains("localhost") || u.Contains("127.0.0.1") || u.Contains("0.0.0.0");
        }

        public static string Recepcion(string? publicBase, string? rnc)
            => Recepcion(publicBase, rnc, "certecf");

        public static string Aprobacion(string? publicBase, string? rnc)
            => Aprobacion(publicBase, rnc, "certecf");

        public static string Autenticacion(string? publicBase, string? rnc)
            => Autenticacion(publicBase, rnc, "certecf");

        public static string RecepcionProd(string? publicBase, string? rnc)
            => Recepcion(publicBase, rnc, "ecf");

        public static string AprobacionProd(string? publicBase, string? rnc)
            => Aprobacion(publicBase, rnc, "ecf");

        public static string AutenticacionProd(string? publicBase, string? rnc)
            => Autenticacion(publicBase, rnc, "ecf");

        public static string Recepcion(string? publicBase, string? rnc, string ambiente)
            => $"{Base(publicBase)}/{Ambiente(ambiente)}/{Digits(rnc)}/fe/recepcion/api/ecf";

        public static string Aprobacion(string? publicBase, string? rnc, string ambiente)
            => $"{Base(publicBase)}/{Ambiente(ambiente)}/{Digits(rnc)}/fe/aprobacioncomercial/api/ecf";

        public static string Autenticacion(string? publicBase, string? rnc, string ambiente)
            => $"{Base(publicBase)}/{Ambiente(ambiente)}/{Digits(rnc)}/fe/autenticacion/api/semilla";

        /// <summary>Carpeta del escritorio con los XML íntegros E32 &lt; 250 mil para Browse + ENVIAR en CerteCF.</summary>
        public static string CarpetaXmlConsumoPortal()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
                desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            return Path.Combine(desktop, "CerteCF-SUBIR-consumo-250mil");
        }

        public static string CarpetaXmlConsumoArtifacts()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents", "GitHub", "AlahiaPosApi", "artifacts", "certecf-consumo-250mil");

        public static string CarpetaRiPortal()
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop))
                desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop");
            return Path.Combine(desktop, "CerteCF-SUBIR-RI");
        }

        public static string CarpetaGoldXml()
            => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Documents", "GitHub", "AlahiaPosApi", "artifacts", "gold-testecf");

        private static string Ambiente(string? a)
        {
            var v = (a ?? "").Trim().ToLowerInvariant();
            return v is "ecf" or "testecf" or "certecf" ? v : "certecf";
        }
    }

    public static class CertecfArtefactos
    {
        public static string TituloRi(int tipo) => tipo switch
        {
            31 => "Factura de Crédito Fiscal Electrónica",
            32 => "Factura de Consumo Electrónica",
            33 => "Nota de Débito Electrónica",
            34 => "Nota de Crédito Electrónica",
            41 => "Compras Electrónica",
            43 => "Gastos Menores Electrónica",
            44 => "Regímenes Especiales Electrónica",
            45 => "Gubernamental Electrónica",
            46 => "Exportaciones Electrónica",
            47 => "Pagos al Exterior Electrónica",
            _ => $"Comprobante Fiscal Electrónico E{tipo}"
        };

        public static string PostulacionXml(Empresas empresa, CertecfPostulacionDto p)
        {
            var rnc = CertecfReceptorUrls.Digits(empresa.RNC);
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("PostulacionEmisorElectronico",
                    new XElement("RNC", rnc),
                    new XElement("RazonSocial", empresa.NombreComercial ?? ""),
                    new XElement("Software",
                        new XElement("Nombre", p.NombreSoftware ?? "Alahia ERP"),
                        new XElement("Version", p.VersionSoftware ?? "1.0"),
                        new XElement("Tipo", p.TipoSoftware ?? "EXTERNO")),
                    new XElement("UrlsPrueba",
                        new XElement("Recepcion", p.UrlRecepcion ?? ""),
                        new XElement("AprobacionComercial", p.UrlAprobacion ?? ""),
                        new XElement("Autenticacion", p.UrlAutenticacion ?? "")),
                    new XElement("Nota",
                        "Copiar estos datos al formulario de postulación en CerteCF, generar el XML oficial, firmarlo con App Firma Digital DGII y subirlo al portal.")));
            return xml.Declaration + Environment.NewLine + xml.Root!.ToString();
        }

        public static string DeclaracionJuradaXml(Empresas empresa, CertecfPostulacionDto p)
        {
            var rnc = CertecfReceptorUrls.Digits(empresa.RNC);
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("DeclaracionJuradaEmisorElectronico",
                    new XElement("RNC", rnc),
                    new XElement("RazonSocial", empresa.NombreComercial ?? ""),
                    new XElement("Representante",
                        new XElement("Nombre", p.RepresentanteNombre ?? ""),
                        new XElement("Cedula", p.RepresentanteCedula ?? "")),
                    new XElement("Software",
                        new XElement("Nombre", p.NombreSoftware ?? "Alahia ERP"),
                        new XElement("Version", p.VersionSoftware ?? "1.0")),
                    new XElement("Juramento",
                        "Declaro bajo fe de juramento que la certificación de emisor electrónico se realizó de manera íntegra, sin acciones fraudulentas o irregularidades, y que el contribuyente se somete a las condiciones normativas de Facturación Electrónica (Ley 32-23, Decreto 587-24 y normativa DGII vigente)."),
                    new XElement("Nota",
                        "Firmar este XML con el certificado del representante (App Firma Digital DGII) y cargarlo en el paso 13 del portal CerteCF. El portal valida que firme el representante de la postulación.")));
            return xml.Declaration + Environment.NewLine + xml.Root!.ToString();
        }

        public static string RepresentacionImpresaHtml(Empresas empresa, FiscalDocumentoElectronico doc, string? qrUrl)
        {
            var e = doc.Encabezado;
            var titulo = TituloRi(e.TipoEcf);
            var rnc = CertecfReceptorUrls.Digits(e.RncEmisor);
            var razon = string.IsNullOrWhiteSpace(e.RazonSocialEmisor) ? (empresa.NombreComercial ?? "") : e.RazonSocialEmisor;
            var dir = string.IsNullOrWhiteSpace(e.DireccionEmisor) ? (empresa.Direccion ?? "") : e.DireccionEmisor;
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>");
            sb.Append(System.Net.WebUtility.HtmlEncode(titulo));
            sb.Append("</title><style>");
            sb.Append("@page{size:A4;margin:14mm}body{font-family:Arial,sans-serif;max-width:720px;margin:0 auto;color:#111;background:#fff}");
            sb.Append("h1{font-size:18px;text-align:center;margin:0 0 4px}h2{font-size:13px;text-align:center;color:#444;margin:0 0 16px}");
            sb.Append("table{width:100%;border-collapse:collapse;font-size:13px}td,th{border-bottom:1px solid #ddd;padding:6px;text-align:left}");
            sb.Append(".tot{text-align:right;font-weight:700}.qr{margin-top:20px;text-align:center;font-size:10px;word-break:break-all}");
            sb.Append(".qr img{display:block;margin:8px auto}.print-hint{font-size:11px;color:#666;margin-top:18px}");
            sb.Append(".warn{background:#fff3cd;border:1px solid #f0c36d;padding:8px;font-size:12px}");
            sb.Append("@media print{body{margin:0}.print-hint,.warn{display:none}}</style></head><body>");
            sb.Append("<h1>").Append(System.Net.WebUtility.HtmlEncode(titulo)).Append("</h1>");
            sb.Append("<h2>Comprobante Fiscal Electrónico · e-CF</h2>");
            sb.Append("<p><strong>").Append(System.Net.WebUtility.HtmlEncode(razon)).Append("</strong><br>");
            sb.Append("RNC: ").Append(rnc).Append("<br>");
            sb.Append(System.Net.WebUtility.HtmlEncode(dir ?? "")).Append("</p>");
            sb.Append("<p>e-NCF: <strong>").Append(System.Net.WebUtility.HtmlEncode(e.Encf)).Append("</strong><br>");
            sb.Append("Fecha: ").Append(e.FechaEmision.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)).Append("<br>");
            sb.Append("Comprador: ").Append(System.Net.WebUtility.HtmlEncode(e.RazonSocialComprador ?? "")).Append(" · RNC ");
            sb.Append(System.Net.WebUtility.HtmlEncode(e.RncComprador ?? "")).Append("</p>");
            sb.Append("<table><thead><tr><th>Item</th><th>Cant.</th><th>Precio</th><th>Monto</th></tr></thead><tbody>");
            foreach (var l in doc.Lineas)
            {
                sb.Append("<tr><td>").Append(System.Net.WebUtility.HtmlEncode(l.NombreItem)).Append("</td>");
                sb.Append("<td>").Append(l.Cantidad.ToString("0.##", CultureInfo.InvariantCulture)).Append("</td>");
                sb.Append("<td>").Append(l.PrecioUnitario.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))).Append("</td>");
                sb.Append("<td>").Append(l.MontoItem.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))).Append("</td></tr>");
            }
            sb.Append("</tbody></table>");
            sb.Append("<p class=\"tot\">ITBIS: ").Append(e.TotalItbis.ToString("N2", CultureInfo.GetCultureInfo("es-DO")));
            sb.Append("<br>Monto total: ").Append(e.MontoTotal.ToString("N2", CultureInfo.GetCultureInfo("es-DO"))).Append("</p>");
            sb.Append("<div class=\"qr\"><strong>ConsultaTimbre</strong>");
            if (!string.IsNullOrWhiteSpace(qrUrl))
            {
                var png = QrPngBase64(qrUrl);
                if (!string.IsNullOrWhiteSpace(png))
                    sb.Append("<img alt=\"QR ConsultaTimbre\" width=\"150\" height=\"150\" src=\"data:image/png;base64,").Append(png).Append("\" />");
                sb.Append("<br>").Append(System.Net.WebUtility.HtmlEncode(qrUrl));
            }
            else
            {
                sb.Append("<p class=\"warn\">Sin QR: falta el XML firmado de esta corrida. No suba este archivo al portal.</p>");
            }
            sb.Append("</div>");
            sb.Append("<p class=\"print-hint\">Chrome → Imprimir → Guardar como PDF. Suba el PDF (no el HTML) en el recuadro del tipo en CerteCF.</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        public static string? QrPngBase64(string content)
        {
            try
            {
                using var gen = new QRCoder.QRCodeGenerator();
                using var data = gen.CreateQrCode(content, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var png = new QRCoder.PngByteQRCode(data).GetGraphic(6);
                return Convert.ToBase64String(png);
            }
            catch
            {
                return null;
            }
        }

        public static string? BuscarXmlFirmado(string encf, string? rncEmisor)
        {
            var e = (encf ?? "").Trim().ToUpperInvariant();
            var rnc = CertecfReceptorUrls.Digits(rncEmisor);
            var names = new[]
            {
                Path.Combine(CertecfReceptorUrls.CarpetaGoldXml(), $"certecf_ECF_{e}_firmado.xml"),
                Path.Combine(CertecfReceptorUrls.CarpetaXmlConsumoArtifacts(), $"{rnc}{e}.xml"),
                Path.Combine(CertecfReceptorUrls.CarpetaXmlConsumoPortal(), $"{rnc}{e}.xml"),
            };
            foreach (var p in names)
            {
                if (File.Exists(p)) return File.ReadAllText(p);
            }
            return null;
        }

        public static string? ExtraerQrDeRespuesta(string? respuesta)
        {
            if (string.IsNullOrWhiteSpace(respuesta)) return null;
            foreach (var line in respuesta.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("QR", StringComparison.OrdinalIgnoreCase)
                    && t.Contains("http", StringComparison.OrdinalIgnoreCase))
                {
                    var i = t.IndexOf("http", StringComparison.OrdinalIgnoreCase);
                    return t[i..].Trim();
                }
            }
            return null;
        }

        public static FiscalDocumentoElectronico Simulacion(Empresas empresa, int tipo, string encf, string? encfModificado = null)
        {
            var rnc = CertecfReceptorUrls.Digits(empresa.RNC);
            var monto = tipo == 32 ? 1250.00m : 8500.00m;
            var itbis = Math.Round(monto * 0.18m / 1.18m, 2);
            var gravado = monto - itbis;
            var doc = new FiscalDocumentoElectronico
            {
                IdEmpresa = empresa.IdEmpresa,
                TipoDocumentoAlahia = "CertificacionSimulacion",
                AmbienteDgii = "certecf",
                Encabezado = new FiscalDocumentoEncabezado
                {
                    TipoEcf = tipo,
                    Encf = encf,
                    TipoIngreso = 1,
                    TipoPago = 1,
                    FechaEmision = DateTime.Today,
                    FechaVencimientoSecuencia = DateTime.Today.AddYears(1),
                    RncEmisor = rnc,
                    RazonSocialEmisor = empresa.NombreComercial ?? "",
                    NombreComercialEmisor = empresa.NombreComercial,
                    DireccionEmisor = empresa.Direccion,
                    MunicipioEmisor = empresa.Municipio,
                    ProvinciaEmisor = empresa.Provincia,
                    TelefonoEmisor = empresa.Telefono,
                    CorreoEmisor = empresa.CorreElectronico,
                    NumeroFacturaInterna = "SIM-CERTECF",
                    RncComprador = tipo is 32 or 43 ? "" : "130877371",
                    RazonSocialComprador = tipo is 32 or 43 ? "CONSUMIDOR FINAL" : "CLIENTE SIMULACION CERTECF",
                    MontoGravadoTotal = gravado,
                    MontoGravadoI1 = gravado,
                    TotalItbis = itbis,
                    TotalItbis1 = itbis,
                    MontoTotal = monto
                }
            };
            doc.Lineas.Add(new FiscalDocumentoLinea
            {
                NumeroLinea = 1,
                IndicadorFacturacion = 1,
                NombreItem = $"Servicio simulación certificación E{tipo}",
                EsBien = false,
                Cantidad = 1,
                PrecioUnitario = gravado,
                MontoItem = gravado
            });
            if (tipo is 33 or 34 && !string.IsNullOrWhiteSpace(encfModificado))
            {
                doc.Referencia = new FiscalDocumentoReferencia
                {
                    NcfModificado = encfModificado,
                    FechaNcfModificado = DateTime.Today,
                    CodigoModificacion = 1,
                    RazonModificacion = "Simulación CerteCF"
                };
            }
            if (tipo != 33 && tipo != 34)
            {
                doc.FormasPago.Add(new FiscalDocumentoFormaPagoDgii { FormaPago = 1, Monto = monto });
            }
            return doc;
        }
    }
}
