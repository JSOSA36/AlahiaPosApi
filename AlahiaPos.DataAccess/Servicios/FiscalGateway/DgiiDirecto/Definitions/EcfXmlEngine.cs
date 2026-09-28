using System.Text;
using System.Xml;
using System.Xml.Linq;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>
    /// Motor genérico: carga definición → valida → emite solo nodos permitidos → XML.
    /// No contiene reglas por tipo; esas viven en IEcfTipoDefinition.
    /// Política: no agregar ramas por TipoeCF aquí — ver docs/dgii-ecf/definiciones/ y .cursor/rules/ecf-motor-definiciones.mdc.
    /// </summary>
    public static class EcfXmlEngine
    {
        public static string Build(FiscalDocumentoElectronico doc, DateTime fechaHoraFirma)
        {
            if (string.Equals(doc.AmbienteDgii, "certecf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(DgiiAmbienteContext.Current, "certecf", StringComparison.OrdinalIgnoreCase))
                CertecfExcelParser.SanearTelefonosCertecf(doc);

            var def = EcfTipoDefinitionRegistry.Get(doc.Encabezado.TipoEcf);
            var ctx = new EcfBuildContext
            {
                Documento = doc,
                Definicion = def,
                FechaHoraFirma = fechaHoraFirma
            };

            def.Validar(ctx);

            var encabezado = new XElement("Encabezado",
                new XElement("Version", "1.0"),
                BuildSeccion("IdDoc", def.IdDoc, ctx),
                BuildSeccion("Emisor", def.Emisor, ctx));
            if (def.Comprador != null)
                encabezado.Add(BuildSeccion("Comprador", def.Comprador, ctx));
            encabezado.Add(BuildSeccion("Totales", def.Totales, ctx));

            var root = new XElement("ECF",
                encabezado,
                BuildDetalles(def, ctx),
                BuildDescuentos(def, ctx),
                BuildReferencia(def, ctx),
                new XElement("FechaHoraFirma", EcfXmlFormat.DateTimeStamp(fechaHoraFirma))
            );

            def.PostProcesar(ctx, root);

            root.Descendants()
                .Where(e => !e.HasElements && string.IsNullOrEmpty(e.Value) && !e.HasAttributes)
                .Remove();

            return ToUtf8Xml(root);
        }

        private static XElement BuildSeccion(string nombre, IReadOnlyList<EcfCampoDef> campos, EcfBuildContext ctx)
        {
            var el = new XElement(nombre);
            AplicarCampos(el, campos, ctx);
            return el;
        }

        private static void AplicarCampos(XElement parent, IReadOnlyList<EcfCampoDef> campos, EcfBuildContext ctx)
        {
            foreach (var campo in campos.OrderBy(c => c.Orden))
            {
                if (campo.Presence == EcfCampoPresence.Prohibido)
                    continue;

                campo.Validar?.Invoke(ctx);
                if (campo.Resolver == null) continue;

                object? valor = null;
                var respetarExcel = EcfXmlFormat.DebeRespetarExcel(ctx) && !campo.EsComplejo;
                if (respetarExcel
                    && campo.Nombre.Equals("TelefonoAdicional", StringComparison.OrdinalIgnoreCase))
                {
                    // El set CerteCF E32 <250k duplica TelefonoEmisor en TelefonoAdicional.
                    // Hay que emitir los dos; omitir el tag hace que DGII lo reclame igual.
                    if (!EcfXmlFormat.TryCeldaExcel(ctx, campo.Nombre, out var adicRaw))
                        continue;
                    var adic = EcfXmlFormat.Telefono(adicRaw);
                    if (adic == null) continue;
                    valor = adic;
                }
                else if (respetarExcel
                    && !EcfXmlFormat.EsCampoCodigo(campo.Nombre)
                    && EcfXmlFormat.TryCeldaExcel(ctx, campo.Nombre, out var excel))
                {
                    valor = excel;
                }
                else if (respetarExcel
                    && campo.Presence == EcfCampoPresence.Opcional
                    && !EcfXmlFormat.EsCampoCodigo(campo.Nombre))
                {
                    // CerteCF: celda vacía = no emitir (no inventar 0.00).
                    continue;
                }
                else
                {
                    try
                    {
                        valor = campo.Resolver(ctx);
                    }
                    catch (Exception ex) when (campo.Presence == EcfCampoPresence.Opcional)
                    {
                        _ = ex;
                        continue;
                    }
                }

                if (valor is null || (valor is string s && string.IsNullOrWhiteSpace(s)))
                {
                    if (campo.Presence == EcfCampoPresence.Obligatorio)
                        throw new InvalidOperationException(
                            $"{ctx.Definicion.Codigo}: {parent.Name}.{campo.Nombre} es obligatorio.");
                    continue;
                }

                if (campo.EsComplejo)
                {
                    if (valor is XElement xe)
                        parent.Add(xe);
                    continue;
                }

                parent.Add(new XElement(campo.Nombre, valor));
            }
        }

        private static XElement BuildDetalles(IEcfTipoDefinition def, EcfBuildContext ctx)
        {
            var detalles = new XElement("DetallesItems");
            foreach (var linea in ctx.Documento.Lineas)
            {
                ctx.LineaActual = linea;
                var item = new XElement("Item");
                AplicarCampos(item, def.Item, ctx);
                detalles.Add(item);
            }
            ctx.LineaActual = null;
            return detalles;
        }

        private static XElement? BuildDescuentos(IEcfTipoDefinition def, EcfBuildContext ctx)
        {
            if (!def.PermiteDescuentosORecargos || ctx.Documento.Descuentos.Count == 0)
                return null;

            var root = new XElement("DescuentosORecargos");
            var respetarExcel = EcfXmlFormat.DebeRespetarExcel(ctx);
            foreach (var d in ctx.Documento.Descuentos)
            {
                var n = d.NumeroLinea;
                var el = new XElement("DescuentoORecargo",
                    new XElement("NumeroLinea", n),
                    new XElement("TipoAjuste", d.EsDescuento ? "D" : "R"));
                if (!string.IsNullOrWhiteSpace(d.Descripcion))
                    el.Add(new XElement("DescripcionDescuentooRecargo", EcfXmlFormat.Esc(d.Descripcion, 45)));
                el.Add(new XElement("TipoValor",
                    EcfXmlFormat.TryCeldaExcel(ctx, "TipoValor", out var tipoValorExcel, n)
                        ? tipoValorExcel
                        : (d.EsMontoFijo ? "$" : "%")));

                // CerteCF: celda vacía de ValorDescuentooRecargo = no emitir (Excel trae Monto, no Valor).
                if (EcfXmlFormat.TryCeldaExcel(ctx, "ValorDescuentooRecargo", out var valorExcel, n))
                    el.Add(new XElement("ValorDescuentooRecargo", valorExcel));
                else if (!respetarExcel)
                    el.Add(new XElement("ValorDescuentooRecargo", EcfXmlFormat.Money(d.Monto)));

                var montoTxt = EcfXmlFormat.TryCeldaExcel(ctx, "MontoDescuentooRecargo", out var montoExcel, n)
                    ? montoExcel
                    : EcfXmlFormat.Money(d.Monto);
                el.Add(new XElement("MontoDescuentooRecargo", montoTxt));
                if (d.IndicadorFacturacion.HasValue)
                    el.Add(new XElement("IndicadorFacturacionDescuentooRecargo", d.IndicadorFacturacion.Value));
                root.Add(el);
            }
            return root;
        }

        private static XElement? BuildReferencia(IEcfTipoDefinition def, EcfBuildContext ctx)
        {
            if (def.InformacionReferencia == null)
                return null;

            if (ctx.Documento.Referencia == null ||
                string.IsNullOrWhiteSpace(ctx.Documento.Referencia.NcfModificado))
            {
                if (def.RequiereInformacionReferencia)
                    throw new InvalidOperationException(
                        $"{def.Codigo}: InformacionReferencia es obligatoria.");
                return null;
            }

            return BuildSeccion("InformacionReferencia", def.InformacionReferencia, ctx);
        }

        private static string ToUtf8Xml(XElement root)
        {
            // XmlWriter sobre StringBuilder fuerza encoding="utf-16". MemoryStream + UTF-8 sin BOM.
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
                var docX = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
                docX.Save(writer);
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
