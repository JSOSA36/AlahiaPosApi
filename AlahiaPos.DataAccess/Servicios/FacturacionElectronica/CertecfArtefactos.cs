using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    public static class CertecfReceptorUrls
    {
        public static string Digits(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        /// <summary>RNC emisor del payload CerteCF (Excel/e-CF), no Empresas.RNC.</summary>
        public static string? ExtraerRncEmisorJson(string? payloadJson)
        {
            if (string.IsNullOrWhiteSpace(payloadJson)) return null;
            try
            {
                using var doc = JsonDocument.Parse(payloadJson);
                var root = doc.RootElement;
                if (TryProp(root, "encabezado", "Encabezado", out var enc)
                    && TryProp(enc, "rncEmisor", "RncEmisor", out var r1))
                {
                    var d = Digits(r1.GetString());
                    if (d.Length >= 9) return d;
                }
                if (TryProp(root, "rncEmisor", "RncEmisor", out var r2))
                {
                    var d = Digits(r2.GetString());
                    if (d.Length >= 9) return d;
                }
            }
            catch
            {
                /* preview best-effort */
            }
            return null;
        }

        private static bool TryProp(JsonElement el, string camel, string pascal, out JsonElement value)
        {
            if (el.ValueKind == JsonValueKind.Object
                && (el.TryGetProperty(camel, out value) || el.TryGetProperty(pascal, out value)))
                return true;
            value = default;
            return false;
        }

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
            41 => "Comprobante Electrónico de Compras",
            43 => "Comprobante Electrónico para Gastos Menores",
            44 => "Comprobante Electrónico para Regímenes Especiales",
            45 => "Comprobante Electrónico Gubernamental",
            46 => "Comprobante Electrónico para Exportaciones",
            47 => "Comprobante Electrónico para Pagos al Exterior",
            _ => $"Comprobante Fiscal Electrónico E{tipo}"
        };

        /// <summary>
        /// Formato e-CF pág. 6 campo 4: E32 y E34 no llevan FechaVencimientoSecuencia.
        /// El resto sí, y la RI debe imprimirla.
        /// </summary>
        public static bool LlevaFechaVencimientoRi(int tipo) => tipo is not 32 and not 34;

        public static bool MostrarClienteRi(int tipo) => tipo is not 32 and not 43;

        public static string UnidadRi(int? codigo) => codigo switch
        {
            43 or 21 or null or 0 => "UND",
            _ => "UND"
        };

        public static (string? codigo, string? fechaFirma) TimbreDesdeQr(string? qrUrl)
        {
            if (string.IsNullOrWhiteSpace(qrUrl)) return (null, null);
            try
            {
                var q = qrUrl.IndexOf('?');
                if (q < 0) return (null, null);
                string? codigo = null, fecha = null;
                foreach (var part in qrUrl[(q + 1)..].Split('&'))
                {
                    var kv = part.Split('=', 2);
                    if (kv.Length != 2) continue;
                    var key = kv[0];
                    var val = Uri.UnescapeDataString(kv[1].Replace("+", " "));
                    if (key.Equals("CodigoSeguridad", StringComparison.OrdinalIgnoreCase)) codigo = val;
                    if (key.Equals("FechaFirma", StringComparison.OrdinalIgnoreCase)) fecha = val;
                }
                return (codigo, fecha);
            }
            catch
            {
                return (null, null);
            }
        }

        public const string RncDraSena = "133659115";
        public const string RazonSocialDraSena = "CENTRO ODONTOLOGICO DRA SENA 1723 SRL";
        public const string NombreComercialDraSena = "CENTRO ODONTOLOGICO DRA SENA 1723";
        public static readonly DateTime FechaVencimientoSecuenciaCertecf = new(2028, 12, 31);

        /// <summary>
        /// Respaldo conocido del padrón. La fuente en vivo es ClientesDGII por el
        /// RNC emisor de ese e-CF (hoy Dra Sena, mañana otro contribuyente).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, CertecfRiEmisor> PadronRiCertecf =
            new Dictionary<string, CertecfRiEmisor>(StringComparer.Ordinal)
            {
                [RncDraSena] = new CertecfRiEmisor
                {
                    Rnc = RncDraSena,
                    RazonSocial = RazonSocialDraSena,
                    NombreComercial = NombreComercialDraSena,
                    Direccion = "Santo Domingo",
                    Telefono = "849-255-6007"
                }
            };

        public static bool EsRncDraSena(string? rnc)
            => CertecfReceptorUrls.Digits(rnc) == RncDraSena;

        /// <summary>
        /// RNC que va en el e-CF. Empresa 60 tiene Empresas.RNC de otro contribuyente;
        /// el certificado y DGII son Dra Sena (133659115). No cambia Empresas.RNC.
        /// </summary>
        public static string RncEmisorParaEmpresa(Empresas? empresa)
        {
            if (empresa == null) return "";
            if (empresa.IdEmpresa == 60) return RncDraSena;
            return CertecfReceptorUrls.Digits(empresa.RNC);
        }

        /// <summary>
        /// Razón social del contribuyente de ese RNC (padrón DGII), nunca el nombre
        /// de muestra del Excel. El fallback de Dra Sena solo aplica si el RNC es el suyo
        /// y el padrón no trajo razón legal.
        /// </summary>
        public static string RazonSocialRealParaRnc(string? rnc, params string?[] candidatas)
        {
            var elegida = ElegirRazonSocial(candidatas);
            if (!string.IsNullOrWhiteSpace(elegida)) return elegida;
            return EsRncDraSena(rnc) ? RazonSocialDraSena : "";
        }

        public static string NombreComercialRealParaRnc(string? rnc, params string?[] candidatas)
        {
            foreach (var raw in candidatas)
            {
                var v = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(v) || EsNombreLaboratorio(v) || EsEtiquetaPrueba(v))
                    continue;
                return v;
            }
            return EsRncDraSena(rnc) ? NombreComercialDraSena : "";
        }

        public static void AplicarIdentidadEmisorReal(
            FiscalDocumentoElectronico doc,
            string? razonDgii = null,
            string? comercialDgii = null)
        {
            if (doc?.Encabezado == null) return;
            var rnc = CertecfReceptorUrls.Digits(doc.Encabezado.RncEmisor);
            var razon = RazonSocialRealParaRnc(
                rnc,
                razonDgii,
                doc.Encabezado.RazonSocialEmisor);
            var comercial = NombreComercialRealParaRnc(
                rnc,
                comercialDgii,
                doc.Encabezado.NombreComercialEmisor,
                razon);
            doc.Encabezado.RazonSocialEmisor = razon;
            doc.Encabezado.NombreComercialEmisor = comercial;
            SetCelda(doc, "razonsocialemisor", razon);
            SetCelda(doc, "nombrecomercial", comercial);
            SetCelda(doc, "nombrecomercialemisor", comercial);
        }

        public static void AsegurarFechaVencimientoSecuenciaCertecf(FiscalDocumentoElectronico doc)
        {
            if (doc?.Encabezado == null) return;
            if (!LlevaFechaVencimientoRi(doc.Encabezado.TipoEcf)) return;
            var fv = doc.Encabezado.FechaVencimientoSecuencia;
            if (fv is null || fv.Value == default
                || fv.Value.Date <= DateTime.Today
                || fv.Value.Year < FechaVencimientoSecuenciaCertecf.Year)
            {
                doc.Encabezado.FechaVencimientoSecuencia = FechaVencimientoSecuenciaCertecf;
            }
        }

        private static void SetCelda(FiscalDocumentoElectronico doc, string key, string value)
        {
            doc.CeldasExcel ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            doc.CeldasExcel[key] = value;
        }

        /// <summary>
        /// CerteCF paso 5 OCR: la Razón Social impresa es la del RNC emisor en el padrón,
        /// no el nombre de muestra del Excel. El nombre comercial va en otra línea.
        /// </summary>
        public static CertecfRiEmisor ResolverEmisorRi(
            Empresas empresa,
            FiscalDocumentoElectronico doc,
            string? xmlFirmado,
            string? razonSocialDgii,
            string? nombreComercialDgii = null)
        {
            var xml = ExtraerEmisorXml(xmlFirmado);
            var e = doc.Encabezado;
            var rnc = FirstDigits(xml?.Rnc, e.RncEmisor, empresa.RNC);

            var razon = RazonSocialRealParaRnc(
                rnc,
                razonSocialDgii,
                xml?.RazonSocial,
                e.RazonSocialEmisor,
                empresa.NombreComercial);

            var comercial = NombreComercialRealParaRnc(
                rnc,
                nombreComercialDgii,
                empresa.NombreComercial,
                xml?.NombreComercial,
                razon);

            var direccion = FirstNonEmpty(empresa.Direccion);
            if (string.IsNullOrWhiteSpace(direccion) || EsDireccionLaboratorio(direccion))
            {
                direccion = FirstNonEmpty(
                    EsDireccionLaboratorio(xml?.Direccion) ? null : xml?.Direccion,
                    EsDireccionLaboratorio(e.DireccionEmisor) ? null : e.DireccionEmisor,
                    empresa.Direccion);
            }

            var telefono = FirstNonEmpty(empresa.Telefono);
            if (string.IsNullOrWhiteSpace(telefono) || EsTelefonoLaboratorio(telefono))
            {
                telefono = FirstNonEmpty(
                    EsTelefonoLaboratorio(xml?.Telefono) ? null : xml?.Telefono,
                    EsTelefonoLaboratorio(e.TelefonoEmisor) ? null : e.TelefonoEmisor,
                    empresa.Telefono);
            }

            return new CertecfRiEmisor
            {
                Rnc = rnc,
                RazonSocial = razon,
                NombreComercial = comercial,
                Direccion = direccion,
                Telefono = telefono,
                Correo = FirstNonEmpty(empresa.CorreElectronico, xml?.Correo, e.CorreoEmisor)
            };
        }

        public static bool EsNombreLaboratorio(string? s)
        {
            var u = (s ?? "").ToUpperInvariant();
            return u.Contains("DOCUMENTOS ELECTRONICOS", StringComparison.Ordinal);
        }

        private static bool EsDireccionLaboratorio(string? s)
        {
            var u = (s ?? "").ToUpperInvariant();
            return u.Contains("ISABEL AGUIAR", StringComparison.Ordinal)
                   || u.Contains("DOCUMENTOS ELECTRONICOS", StringComparison.Ordinal);
        }

        private static bool EsTelefonoLaboratorio(string? s)
        {
            var d = CertecfReceptorUrls.Digits(s);
            return d is "8094727676" or "8094911918";
        }

        public static bool EsRazonSocialLegal(string? s)
        {
            var u = (s ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(u) || EsNombreLaboratorio(u) || EsEtiquetaPrueba(u))
                return false;
            return u.Contains(" SRL") || u.EndsWith("SRL")
                || u.Contains(" S.R.L") || u.Contains(" SA") || u.EndsWith(" SA")
                || u.Contains(" S.A") || u.Contains(" EIRL") || u.Contains(" SAS");
        }

        private static string ElegirRazonSocial(params string?[] candidatas)
        {
            string? legal = null;
            string? otra = null;
            foreach (var raw in candidatas)
            {
                var v = (raw ?? "").Trim();
                if (string.IsNullOrWhiteSpace(v) || EsEtiquetaPrueba(v) || EsNombreLaboratorio(v))
                    continue;
                if (EsRazonSocialLegal(v))
                {
                    legal ??= v;
                    continue;
                }
                otra ??= v;
            }
            return legal ?? otra ?? "";
        }

        private static bool EsEtiquetaPrueba(string? s)
        {
            var u = (s ?? "").ToUpperInvariant();
            return u.Contains("PRUEBA", StringComparison.Ordinal)
                   || u.Contains("250MIL", StringComparison.Ordinal)
                   || u.Contains("MENOR 250", StringComparison.Ordinal);
        }

        private static string DireccionUsable(string? candidata, string canon, string razonSocial)
        {
            if (string.IsNullOrWhiteSpace(candidata)) return canon;
            var d = candidata.Trim();
            if (string.Equals(d, razonSocial, StringComparison.OrdinalIgnoreCase)) return canon;
            if (d.StartsWith("DOCUMENTOS ELECTRONICOS", StringComparison.OrdinalIgnoreCase)
                && d.IndexOf("AVE", StringComparison.OrdinalIgnoreCase) < 0)
                return canon;
            return d;
        }

        public static CertecfRiEmisor? ExtraerEmisorXml(string? xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return null;
            try
            {
                var x = XDocument.Parse(xml);
                string Val(string name) =>
                    x.Descendants().FirstOrDefault(n => n.Name.LocalName == name)?.Value?.Trim() ?? "";
                var rnc = CertecfReceptorUrls.Digits(Val("RNCEmisor"));
                var razon = Val("RazonSocialEmisor");
                if (string.IsNullOrWhiteSpace(rnc) && string.IsNullOrWhiteSpace(razon)) return null;
                return new CertecfRiEmisor
                {
                    Rnc = rnc,
                    RazonSocial = razon,
                    NombreComercial = Val("NombreComercial"),
                    Direccion = Val("DireccionEmisor"),
                    Telefono = Val("TelefonoEmisor"),
                    Correo = Val("CorreoEmisor")
                };
            }
            catch
            {
                return null;
            }
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var v in values)
            {
                if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
            }
            return "";
        }

        private static string FirstDigits(params string?[] values)
        {
            foreach (var v in values)
            {
                var d = CertecfReceptorUrls.Digits(v);
                if (!string.IsNullOrWhiteSpace(d)) return d;
            }
            return "";
        }

        public static string PostulacionXml(Empresas empresa, CertecfPostulacionDto p, string? rncCertecf = null, string? razonSocial = null)
        {
            var rnc = FirstDigits(rncCertecf, empresa.RNC);
            var razon = string.IsNullOrWhiteSpace(razonSocial) ? (empresa.NombreComercial ?? "") : razonSocial.Trim();
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("PostulacionEmisorElectronico",
                    new XElement("RNC", rnc),
                    new XElement("RazonSocial", razon),
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

        public static string DeclaracionJuradaXml(Empresas empresa, CertecfPostulacionDto p, string? rncCertecf = null, string? razonSocial = null)
        {
            var rnc = FirstDigits(rncCertecf, empresa.RNC);
            var razon = string.IsNullOrWhiteSpace(razonSocial) ? (empresa.NombreComercial ?? "") : razonSocial.Trim();
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null),
                new XElement("DeclaracionJuradaEmisorElectronico",
                    new XElement("RNC", rnc),
                    new XElement("RazonSocial", razon),
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

        public static string RepresentacionImpresaHtml(Empresas empresa, FiscalDocumentoElectronico doc, string? qrUrl, string? razonSocialDgii = null)
        {
            var emisor = ResolverEmisorRi(empresa, doc, null, razonSocialDgii);
            return RepresentacionImpresaHtml(emisor, doc, qrUrl);
        }

        public static string RepresentacionImpresaHtml(CertecfRiEmisor emisor, FiscalDocumentoElectronico doc, string? qrUrl)
            => RepresentacionImpresaHtml(emisor, doc, qrUrl, null, null);

        public static string RepresentacionImpresaHtml(
            CertecfRiEmisor emisor, FiscalDocumentoElectronico doc, string? qrUrl, string? codigoSeguridad, string? fechaFirma)
        {
            var e = doc.Encabezado;
            var titulo = TituloRi(e.TipoEcf);
            var es = CultureInfo.GetCultureInfo("es-DO");
            string H(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            var (codigoQr, fechaQr) = TimbreDesdeQr(qrUrl);
            var codigo = string.IsNullOrWhiteSpace(codigoSeguridad) ? codigoQr : codigoSeguridad;
            var firma = string.IsNullOrWhiteSpace(fechaFirma) ? fechaQr : fechaFirma;

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\"><title>");
            sb.Append(H(titulo));
            sb.Append("</title><style>");
            sb.Append("@page{size:A4;margin:12mm}body{font-family:Arial,sans-serif;max-width:720px;margin:0 auto;color:#111;background:#fff}");
            sb.Append("h1{font-size:16px;text-align:center;margin:0 0 4px}");
            sb.Append(".idoc{text-align:center;font-size:13px;margin:0 0 2px}");
            sb.Append("table{width:100%;border-collapse:collapse;font-size:12px;margin-top:12px}td,th{border-bottom:1px solid #ddd;padding:5px;text-align:left}");
            sb.Append(".lab{color:#444;width:160px}.tot{text-align:right;font-weight:700}");
            sb.Append(".qr{margin-top:16px;text-align:center;font-size:11px}");
            sb.Append(".qr img{display:block;margin:8px auto}.qr p{margin:4px 0 0;white-space:nowrap}");
            sb.Append("</style></head><body>");
            sb.Append("<h1>").Append(H(titulo)).Append("</h1>");
            sb.Append("<p class=\"idoc\">e-NCF: <strong>").Append(H(e.Encf)).Append("</strong></p>");
            if (LlevaFechaVencimientoRi(e.TipoEcf) && e.FechaVencimientoSecuencia is DateTime fvHtml && fvHtml != default)
                sb.Append("<p class=\"idoc\">Fecha Vencimiento: ").Append(fvHtml.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)).Append("</p>");
            sb.Append("<table>");
            sb.Append("<tr><td class=\"lab\">Razón Social</td><td><strong>").Append(H(emisor.RazonSocial)).Append("</strong></td></tr>");
            sb.Append("<tr><td class=\"lab\">Nombre Comercial</td><td>").Append(H(emisor.NombreComercial)).Append("</td></tr>");
            sb.Append("<tr><td class=\"lab\">RNC</td><td>").Append(H(emisor.Rnc)).Append("</td></tr>");
            sb.Append("<tr><td class=\"lab\">Dirección</td><td>").Append(H(emisor.Direccion)).Append("</td></tr>");
            if (!string.IsNullOrWhiteSpace(emisor.Telefono))
                sb.Append("<tr><td class=\"lab\">Teléfono</td><td>").Append(H(emisor.Telefono)).Append("</td></tr>");
            sb.Append("<tr><td class=\"lab\">Fecha de emisión</td><td>").Append(e.FechaEmision.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)).Append("</td></tr>");
            sb.Append("<tr><td class=\"lab\">Razón Social comprador</td><td>").Append(H(e.RazonSocialComprador)).Append("</td></tr>");
            sb.Append("<tr><td class=\"lab\">RNC comprador</td><td>").Append(H(e.RncComprador)).Append("</td></tr>");
            sb.Append("</table>");
            sb.Append("<table style=\"margin-top:12px\"><thead><tr><th>Item</th><th>Cant.</th><th>Precio</th><th>Monto</th></tr></thead><tbody>");
            foreach (var l in doc.Lineas)
            {
                sb.Append("<tr><td>").Append(H(l.NombreItem)).Append("</td>");
                sb.Append("<td>").Append(l.Cantidad.ToString("0.##", CultureInfo.InvariantCulture)).Append("</td>");
                sb.Append("<td>").Append(l.PrecioUnitario.ToString("N2", es)).Append("</td>");
                sb.Append("<td>").Append(l.MontoItem.ToString("N2", es)).Append("</td></tr>");
            }
            sb.Append("</tbody></table>");
            sb.Append("<p class=\"tot\">");
            if (e.MontoNoFacturable is decimal noFacturable && noFacturable != 0)
                sb.Append("Monto no facturable: ").Append(noFacturable.ToString("N2", es)).Append("<br>");
            sb.Append("ITBIS: ").Append(e.TotalItbis.ToString("N2", es));
            sb.Append("<br>Monto total: ").Append(e.MontoTotal.ToString("N2", es)).Append("</p>");
            sb.Append("<div class=\"qr\"><strong>ConsultaTimbre</strong>");
            if (!string.IsNullOrWhiteSpace(qrUrl))
            {
                var png = QrPngBase64(qrUrl);
                if (!string.IsNullOrWhiteSpace(png))
                    sb.Append("<img alt=\"QR ConsultaTimbre\" width=\"150\" height=\"150\" src=\"data:image/png;base64,").Append(png).Append("\" />");
            }
            sb.Append("<p>Código de Seguridad: ").Append(H(codigo)).Append("</p>");
            sb.Append("<p>Fecha Firma: ").Append(H(firma)).Append("</p></div>");
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
                    FechaVencimientoSecuencia = FechaVencimientoSecuenciaCertecf,
                    RncEmisor = rnc,
                    RazonSocialEmisor = RazonSocialRealParaRnc(rnc, empresa.NombreComercial),
                    NombreComercialEmisor = NombreComercialRealParaRnc(rnc, empresa.NombreComercial),
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

    public sealed class CertecfRiEmisor
    {
        public string Rnc { get; init; } = "";
        public string RazonSocial { get; init; } = "";
        public string NombreComercial { get; init; } = "";
        public string Direccion { get; init; } = "";
        public string Telefono { get; init; } = "";
        public string Correo { get; init; } = "";
    }
}
