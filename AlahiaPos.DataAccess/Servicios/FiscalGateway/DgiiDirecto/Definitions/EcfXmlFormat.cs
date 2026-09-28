using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Helpers compartidos de formato/resolución para definiciones e-CF.</summary>
    public static class EcfXmlFormat
    {
        public static string Money(decimal v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>
        /// CerteCF compara texto con el Excel. Entero → "24". Subcantidad puede llevar 3 decimales (0.355 L).
        /// </summary>
        public static string DecimalComoDato(decimal v)
            => v.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Tasa CerteCF: Excel pone <c>10</c>, no <c>10.00</c>.
        /// <see cref="decimal.ToString()"/> conserva escala JSON (10.00m → "10.00").
        /// No usar en cantidades/precios (ahí Excel pide 15.00 / 400.00).
        /// </summary>
        public static string TasaComoDato(decimal v)
            => v == decimal.Truncate(v)
                ? decimal.Truncate(v).ToString(CultureInfo.InvariantCulture)
                : DecimalComoDato(v);

        private static bool EsCertecf(EcfBuildContext ctx)
        {
            var amb = (ctx.Documento.AmbienteDgii ?? DgiiAmbienteContext.Current ?? "")
                .Trim().ToLowerInvariant();
            return amb is "certecf" or "cert" or "certificacion";
        }

        public static bool DebeRespetarExcel(EcfBuildContext ctx)
            => EsCertecf(ctx) && ctx.Documento.CeldasExcel is { Count: > 0 };

        private static readonly HashSet<string> CamposCodigoXml = new(StringComparer.OrdinalIgnoreCase)
        {
            "TipoeCF", "eNCF", "TipoIngresos", "TipoPago",
            "IndicadorMontoGravado", "IndicadorNotaCredito",
            "IndicadorEnvioDiferido", "IndicadorServicioTodoIncluido",
            "IndicadorFacturacion", "IndicadorBienoServicio", "NumeroLinea",
            "UnidadMedida", "UnidadReferencia", "CodigoSubcantidad",
            "FormaPago", "CodigoModificacion", "TipoImpuesto",
            "RNCEmisor", "RNCComprador", "RNCOtroContribuyente"
        };

        public static bool EsCampoCodigo(string nombre) => CamposCodigoXml.Contains(nombre);

        public static string ClaveExcel(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD))
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        public static bool TryCeldaExcel(EcfBuildContext ctx, string xmlNombre, out string texto, int? lineaFija = null)
        {
            texto = "";
            var d = ctx.Documento.CeldasExcel;
            if (d == null || d.Count == 0 || string.IsNullOrWhiteSpace(xmlNombre)) return false;
            var n = lineaFija ?? ctx.LineaActual?.NumeroLinea;
            if (n is int linea && linea > 0)
            {
                if (TryClave(d, ClaveExcel($"L{linea}.{xmlNombre}"), out texto)) return true;
                if (TryClave(d, ClaveExcel(xmlNombre + linea), out texto)) return true;
            }
            return TryClave(d, ClaveExcel(xmlNombre), out texto);
        }

        private static bool TryClave(Dictionary<string, string> d, string key, out string texto)
        {
            texto = "";
            if (string.IsNullOrEmpty(key)) return false;
            if (!d.TryGetValue(key, out var v) || string.IsNullOrWhiteSpace(v)) return false;
            texto = v.Trim();
            return true;
        }

        public static string CantidadOPrecio(EcfBuildContext ctx, decimal v)
            => Money(v);
        public static string Date(DateTime d) => d.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        public static string DateTimeStamp(DateTime d) => d.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        public static string TipoIngreso(int t) => t.ToString("00", CultureInfo.InvariantCulture);

        public static string Esc(string? s, int max)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "";
            return s.Length <= max ? s : s[..max];
        }

        public static string NormalizarRnc(string? rnc)
        {
            var d = new string((rnc ?? "").Where(char.IsDigit).ToArray());
            if (d.Length == 9 || d.Length == 11) return d;
            if (d.Length < 9) return d.PadLeft(9, '0');
            if (d.Length == 10) return d.PadLeft(11, '0');
            return d.Length > 11 ? d[^11..] : d;
        }

        public static string? Telefono(string? tel)
        {
            if (string.IsNullOrWhiteSpace(tel)) return null;
            var d = new string(tel.Where(char.IsDigit).ToArray());
            if (d.Length == 10) return $"{d[..3]}-{d.Substring(3, 3)}-{d.Substring(6, 4)}";
            if (d.Length == 11 && d.StartsWith("1"))
                return $"{d.Substring(1, 3)}-{d.Substring(4, 3)}-{d.Substring(7, 4)}";
            return null;
        }

        public static bool CorreoOk(string? correo)
            => !string.IsNullOrWhiteSpace(correo) &&
               Regex.IsMatch(correo.Trim(), @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");

        public static bool CodigoProvMun(string? code)
            => !string.IsNullOrWhiteSpace(code) && Regex.IsMatch(code.Trim(), @"^\d{6}$");

        /// <summary>
        /// En producción hay que enviar la fecha autorizada por DGII.
        /// El piso 31-12-2028 solo aplica en testecf/certecf (precertificación).
        /// </summary>
        public static DateTime FechaVencimientoPrecert(DateTime? fv)
        {
            var d = (fv ?? new DateTime(2028, 12, 31)).Date;
            var amb = DgiiAmbienteContext.Current;
            var esPrueba = string.Equals(amb, DgiiAmbienteHelper.Pruebas, StringComparison.OrdinalIgnoreCase)
                           || string.Equals(amb, DgiiAmbienteHelper.Certificacion, StringComparison.OrdinalIgnoreCase);
            if (esPrueba && d < new DateTime(2028, 12, 31))
                return new DateTime(2028, 12, 31);
            return d;
        }

        public static EcfCampoDef Campo(
            string nombre,
            EcfCampoPresence presence,
            int orden,
            Func<EcfBuildContext, object?>? resolver = null,
            Action<EcfBuildContext>? validar = null,
            string? nota = null,
            bool complejo = false,
            EcfProhibidoModo prohibidoModo = EcfProhibidoModo.Omitir)
            => new()
            {
                Nombre = nombre,
                Presence = presence,
                Orden = orden,
                Resolver = resolver,
                Validar = validar,
                Nota = nota,
                EsComplejo = complejo,
                ProhibidoModo = prohibidoModo
            };

        public static XElement? TablaFormasPago(EcfBuildContext ctx, int max = 7)
        {
            if (ctx.Documento.FormasPago.Count == 0) return null;
            var tabla = new XElement("TablaFormasPago");
            foreach (var f in ctx.Documento.FormasPago.Take(max))
            {
                tabla.Add(new XElement("FormaDePago",
                    new XElement("FormaPago", f.FormaPago),
                    new XElement("MontoPago", Money(f.Monto))));
            }
            return tabla.HasElements ? tabla : null;
        }

        public static XElement? TablaTelefono(EcfBuildContext ctx)
        {
            if (DebeRespetarExcel(ctx))
            {
                var tabla = new XElement("TablaTelefonoEmisor");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 1; i <= 3; i++)
                {
                    var raw = "";
                    if (TryCeldaExcel(ctx, "TelefonoEmisor" + i, out var excel))
                        raw = excel;
                    else if (i == 1 && TryCeldaExcel(ctx, "TelefonoEmisor", out excel))
                        raw = excel;
                    var telExcel = Telefono(raw);
                    if (telExcel == null || !seen.Add(telExcel)) continue;
                    tabla.Add(new XElement("TelefonoEmisor", telExcel));
                }
                return tabla.HasElements ? tabla : null;
            }

            var tel = Telefono(ctx.Enc.TelefonoEmisor);
            if (tel == null) return null;
            return new XElement("TablaTelefonoEmisor", new XElement("TelefonoEmisor", tel));
        }

        private static string Digitos(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

        /// <summary>Nodo Retencion de ítem (E41: minOccurs=1, orden antes de NombreItem).</summary>
        /// <param name="emitirMontosAunqueCero">
        /// Si true, emite MontoITBISRetenido y MontoISRRetenido aunque sean 0
        /// (hallazgo testecf E41: omisión de MontoITBISRetenido → rechazo).
        /// </param>
        /// <param name="soloIsr">
        /// Si true (E47), solo emite MontoISRRetenido (obligatorio en XSD; sin MontoITBISRetenido).
        /// </param>
        public static XElement RetencionItem(EcfBuildContext ctx, bool emitirMontosAunqueCero = false, bool soloIsr = false)
        {
            var l = ctx.LineaActual
                ?? throw new InvalidOperationException("RetencionItem requiere LineaActual.");
            var ind = l.IndicadorAgenteRetencionoPercepcion ?? 1;
            if (ind is not (1 or 2))
                throw new InvalidOperationException("IndicadorAgenteRetencionoPercepcion debe ser 1 o 2.");

            var el = new XElement("Retencion",
                new XElement("IndicadorAgenteRetencionoPercepcion", ind));

            var itbis = l.MontoItbisRetenido ?? 0m;
            var isr = l.MontoIsrRetenido ?? 0m;

            if (soloIsr)
            {
                el.Add(new XElement("MontoISRRetenido", Money(isr)));
                return el;
            }

            if (emitirMontosAunqueCero || itbis > 0)
                el.Add(new XElement("MontoITBISRetenido", Money(itbis)));
            if (emitirMontosAunqueCero || isr > 0)
                el.Add(new XElement("MontoISRRetenido", Money(isr)));
            return el;
        }

        /// <summary>
        /// Totales/ImpuestosAdicionales. Solo si el Excel trae TipoImpuesto y Tasa &gt; 0.
        /// MontoImpuestoAdicional va en nodo aparte; esta tabla detalla ISC.
        /// </summary>
        public static XElement? ImpuestosAdicionales(EcfBuildContext ctx)
        {
            var list = ctx.Enc.ImpuestosAdicionales;
            if (list == null || list.Count == 0) return null;
            var root = new XElement("ImpuestosAdicionales");
            var n = 0;
            foreach (var i in list)
            {
                var tipo = NormalizarTipoImpuesto(i.TipoImpuesto);
                if (tipo == null || i.TasaImpuestoAdicional <= 0) continue;
                n++;
                var tasaNombre = n == 1 ? "TasaImpuestoAdicional" : $"TasaImpuestoAdicional{n}";
                var tasaTxt = TryCeldaExcel(ctx, tasaNombre, out var tasaExcel)
                    ? tasaExcel
                    : TasaComoDato(i.TasaImpuestoAdicional);
                var nodo = new XElement("ImpuestoAdicional",
                    new XElement("TipoImpuesto", tipo),
                    new XElement("TasaImpuestoAdicional", tasaTxt));

                var espTxt = TextoMontoExcelOTyped(ctx,
                    n == 1 ? "MontoImpuestoSelectivoConsumoEspecifico" : $"MontoImpuestoSelectivoConsumoEspecifico{n}",
                    "MontoImpuestoSelectivoConsumoEspecifico",
                    i.MontoImpuestoSelectivoConsumoEspecifico);
                if (espTxt != null)
                    nodo.Add(new XElement("MontoImpuestoSelectivoConsumoEspecifico", espTxt));

                var advTxt = TextoMontoExcelOTyped(ctx,
                    n == 1 ? "MontoImpuestoSelectivoConsumoAdvalorem" : $"MontoImpuestoSelectivoConsumoAdvalorem{n}",
                    "MontoImpuestoSelectivoConsumoAdvalorem",
                    i.MontoImpuestoSelectivoConsumoAdvalorem);
                if (advTxt != null)
                    nodo.Add(new XElement("MontoImpuestoSelectivoConsumoAdvalorem", advTxt));

                var otrTxt = TextoMontoExcelOTyped(ctx,
                    n == 1 ? "OtrosImpuestosAdicionales" : $"OtrosImpuestosAdicionales{n}",
                    "OtrosImpuestosAdicionales",
                    i.OtrosImpuestosAdicionales);
                if (otrTxt != null)
                    nodo.Add(new XElement("OtrosImpuestosAdicionales", otrTxt));

                root.Add(nodo);
            }
            return root.HasElements ? root : null;
        }

        private static string? TextoMontoExcelOTyped(
            EcfBuildContext ctx, string claveN, string clave, decimal? typed)
        {
            if (TryCeldaExcel(ctx, claveN, out var a) || TryCeldaExcel(ctx, clave, out a))
                return a;
            return typed is > 0 and var v ? Money(v) : null;
        }

        public static XElement? TablaSubcantidadItem(EcfBuildContext ctx)
        {
            var l = ctx.LineaActual;
            if (l == null) return null;

            TryCeldaExcel(ctx, "Subcantidad", out var subExcel);
            TryCeldaExcel(ctx, "CodigoSubcantidad", out var codExcel);

            var item = new XElement("SubcantidadItem");
            if (!string.IsNullOrWhiteSpace(subExcel))
                item.Add(new XElement("Subcantidad", subExcel.Trim()));
            else if (l.Subcantidad is decimal sc)
                item.Add(new XElement("Subcantidad", DecimalComoDato(sc)));

            if (!string.IsNullOrWhiteSpace(codExcel))
                item.Add(new XElement("CodigoSubcantidad", codExcel.Trim()));
            else if (l.CodigoSubcantidad is int cod && cod > 0)
                item.Add(new XElement("CodigoSubcantidad", cod));

            if (!item.HasElements) return null;
            return new XElement("TablaSubcantidad", item);
        }

        /// <summary>
        /// CerteCF E41 010: DGII rechaza DescuentoMonto si falta TablaSubDescuento.
        /// Excel: TipoSubDescuento11 / MontoSubDescuento11 / SubDescuentoPorcentaje21.
        /// </summary>
        public static XElement? TablaSubDescuentoItem(EcfBuildContext ctx)
            => TablaSubAjusteItem(
                ctx,
                tablaNombre: "TablaSubDescuento",
                nodoNombre: "SubDescuento",
                tipoXml: "TipoSubDescuento",
                pctXml: "SubDescuentoPorcentaje",
                montoXml: "MontoSubDescuento",
                typed: ctx.LineaActual?.SubDescuentos,
                montoLinea: ctx.LineaActual?.DescuentoMonto,
                celdaMontoLinea: "DescuentoMonto");

        /// <summary>Igual que TablaSubDescuento, para RecargoMonto.</summary>
        public static XElement? TablaSubRecargoItem(EcfBuildContext ctx)
            => TablaSubAjusteItem(
                ctx,
                tablaNombre: "TablaSubRecargo",
                nodoNombre: "SubRecargo",
                tipoXml: "TipoSubRecargo",
                pctXml: "SubRecargoPorcentaje",
                montoXml: "MontoSubRecargo",
                typed: ctx.LineaActual?.SubRecargos,
                montoLinea: ctx.LineaActual?.RecargoMonto,
                celdaMontoLinea: "RecargoMonto");

        private static XElement? TablaSubAjusteItem(
            EcfBuildContext ctx,
            string tablaNombre,
            string nodoNombre,
            string tipoXml,
            string pctXml,
            string montoXml,
            IList<FiscalSubDescuentoRecargo>? typed,
            decimal? montoLinea,
            string celdaMontoLinea)
        {
            var linea = ctx.LineaActual?.NumeroLinea ?? 0;
            if (linea <= 0) return null;

            var tabla = new XElement(tablaNombre);
            for (var s = 1; s <= 12; s++)
            {
                var tipoTxt = CeldaItemSub(ctx, linea, s, tipoXml);
                var pctTxt = CeldaItemSub(ctx, linea, s, pctXml);
                var montoTxt = CeldaItemSub(ctx, linea, s, montoXml);
                var dto = typed != null && s - 1 < typed.Count ? typed[s - 1] : null;

                if (string.IsNullOrWhiteSpace(tipoTxt)
                    && string.IsNullOrWhiteSpace(pctTxt)
                    && string.IsNullOrWhiteSpace(montoTxt)
                    && dto == null)
                    continue;

                var tipo = NormalizarTipoDescuentoRecargo(
                    !string.IsNullOrWhiteSpace(tipoTxt) ? tipoTxt : dto?.Tipo) ?? "$";
                var sub = new XElement(nodoNombre, new XElement(tipoXml, tipo));
                if (!string.IsNullOrWhiteSpace(pctTxt))
                    sub.Add(new XElement(pctXml, pctTxt.Trim()));
                else if (dto?.Porcentaje is > 0 and var p)
                    sub.Add(new XElement(pctXml, Money(p)));
                if (!string.IsNullOrWhiteSpace(montoTxt))
                    sub.Add(new XElement(montoXml, montoTxt.Trim()));
                else if (dto?.Monto is > 0 and var m)
                    sub.Add(new XElement(montoXml, Money(m)));
                tabla.Add(sub);
            }

            if (!tabla.HasElements && montoLinea is > 0)
            {
                var montoTxt = TryCeldaExcel(ctx, celdaMontoLinea, out var excel)
                    ? excel.Trim()
                    : Money(montoLinea.Value);
                tabla.Add(new XElement(nodoNombre,
                    new XElement(tipoXml, "$"),
                    new XElement(montoXml, montoTxt)));
            }

            return tabla.HasElements ? tabla : null;
        }

        private static string? CeldaItemSub(EcfBuildContext ctx, int linea, int sub, string xmlNombre)
        {
            var d = ctx.Documento.CeldasExcel;
            if (d == null || d.Count == 0) return null;
            var campo = ClaveExcel(xmlNombre);
            foreach (var key in new[]
            {
                campo + linea + sub,
                "l" + linea + sub + campo,
                "l" + linea + campo + sub
            })
            {
                if (TryClave(d, key, out var texto))
                    return texto;
            }
            return null;
        }

        private static string? NormalizarTipoDescuentoRecargo(string? raw)
        {
            var t = (raw ?? "").Trim();
            if (t == "%" || t.Equals("porcentaje", StringComparison.OrdinalIgnoreCase)) return "%";
            if (t == "$" || t.Equals("monto", StringComparison.OrdinalIgnoreCase)) return "$";
            return null;
        }

        public static XElement? TablaImpuestoAdicionalItem(EcfBuildContext ctx)
        {
            var tipos = new List<string>();
            void Add(string? raw)
            {
                var t = NormalizarTipoImpuesto(raw);
                if (t != null && !tipos.Contains(t)) tipos.Add(t);
            }

            Add(ctx.LineaActual?.TipoImpuestoAdicional);
            if (ctx.Enc.ImpuestosAdicionales != null)
            {
                foreach (var i in ctx.Enc.ImpuestosAdicionales)
                    Add(i.TipoImpuesto);
            }

            if (tipos.Count == 0) return null;
            var tabla = new XElement("TablaImpuestoAdicional");
            foreach (var t in tipos.Take(2))
                tabla.Add(new XElement("ImpuestoAdicional", new XElement("TipoImpuesto", t)));
            return tabla;
        }

        public static string? NormalizarTipoImpuesto(string? raw)
        {
            var d = new string((raw ?? "").Where(char.IsDigit).ToArray());
            if (d.Length == 0) return null;
            return d.Length >= 3 ? d[^3..] : d.PadLeft(3, '0');
        }
    }
}
