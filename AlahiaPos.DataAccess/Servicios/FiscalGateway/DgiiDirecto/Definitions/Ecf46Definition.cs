namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Comprobante de Exportaciones Electrónico (E46).</summary>
    public sealed class Ecf46Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 46;
        public override string Codigo => "E46";
        public override string Nombre => "Comprobante de Exportaciones Electrónico";
        public override string XsdArchivo => "e-CF 46 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;

        public override IReadOnlyList<EcfCampoDef>? Comprador { get; } =
            BuildCompradorComun(forzarRncVacio: false, rncObligatorio: false);

        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE46();
        public override IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemE46();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "TipoIngresos y TipoPago obligatorios.",
            "IndicadorMontoGravado NO existe en XSD E46.",
            "TablaFormasPago opcional.",
            "Hallazgo testecf: RNCComprador es obligatorio en práctica (XSD lo marca opcional).",
            "Totales: solo MontoGravadoTotal / MontoGravadoI3 / ITBIS3 / TotalITBIS / TotalITBIS3 / MontoTotal.",
            "IndicadorFacturacion debe ser 3 (ITBIS tasa cero) — Formato DGII nota 51.",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf46Definition()
        {
            var id = IdDocInicio(46);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(EcfXmlFormat.Campo("IndicadorMontoGravado", EcfCampoPresence.Prohibido, 4,
                nota: "No existe en XSD E46"));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Obligatorio, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Obligatorio, 6));
            id.Add(CampoFechaLimitePago(7));
            id.Add(CampoTerminoPago(8));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 9));
            id.Add(CampoTipoCuentaPago(10));
            id.Add(CampoNumeroCuentaPago(11));
            id.Add(CampoBancoPago(12));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            if (string.IsNullOrWhiteSpace(ctx.Enc.RncComprador)
                && string.IsNullOrWhiteSpace(ctx.Enc.IdentificadorExtranjero))
                throw new InvalidOperationException(
                    "E46: RNCComprador o IdentificadorExtranjero es obligatorio.");

            foreach (var l in ctx.Documento.Lineas)
            {
                if (l.IndicadorFacturacion != 3)
                    throw new InvalidOperationException(
                        $"E46: Item {l.NumeroLinea} IndicadorFacturacion debe ser 3 (ITBIS tasa cero).");
            }

            if (ctx.Enc.MontoTotal <= 0)
                throw new InvalidOperationException("E46: MontoTotal debe ser > 0.");
        }

        private static IReadOnlyList<EcfCampoDef> BuildTotalesE46()
        {
            return new[]
            {
                EcfXmlFormat.Campo("MontoGravadoTotal", EcfCampoPresence.Opcional, 1,
                    c =>
                    {
                        var g = c.Enc.MontoGravadoTotal > 0 ? c.Enc.MontoGravadoTotal
                            : (c.Enc.MontoGravadoI3 > 0 ? c.Enc.MontoGravadoI3 : c.Enc.MontoTotal);
                        return g > 0 ? EcfXmlFormat.Money(g) : null;
                    }),
                EcfXmlFormat.Campo("MontoGravadoI3", EcfCampoPresence.Opcional, 2,
                    c =>
                    {
                        var g = c.Enc.MontoGravadoI3 > 0 ? c.Enc.MontoGravadoI3
                            : (c.Enc.MontoGravadoTotal > 0 ? c.Enc.MontoGravadoTotal : c.Enc.MontoTotal);
                        return g > 0 ? EcfXmlFormat.Money(g) : null;
                    },
                    nota: "Exportación: gravado a tasa cero (I3)"),
                EcfXmlFormat.Campo("ITBIS3", EcfCampoPresence.Opcional, 3,
                    c => (c.Enc.MontoGravadoI3 > 0 || c.Enc.MontoGravadoTotal > 0 || c.Enc.MontoTotal > 0) ? 0 : null,
                    nota: "Tasa 0%"),
                EcfXmlFormat.Campo("TotalITBIS", EcfCampoPresence.Opcional, 4,
                    c => EcfXmlFormat.Money(c.Enc.TotalItbis)),
                EcfXmlFormat.Campo("TotalITBIS3", EcfCampoPresence.Opcional, 5,
                    c => EcfXmlFormat.Money(c.Enc.TotalItbis3 > 0 ? c.Enc.TotalItbis3 : c.Enc.TotalItbis)),
                EcfXmlFormat.Campo("MontoTotal", EcfCampoPresence.Obligatorio, 6,
                    c => EcfXmlFormat.Money(c.Enc.MontoTotal)),
                EcfXmlFormat.Campo("MontoPeriodo", EcfCampoPresence.Opcional, 7,
                    c => c.Enc.MontoPeriodo is decimal mp ? EcfXmlFormat.Money(mp) : null),
                EcfXmlFormat.Campo("ValorPagar", EcfCampoPresence.Opcional, 8,
                    c => c.Enc.ValorPagar is decimal vp ? EcfXmlFormat.Money(vp) : null),
                EcfXmlFormat.Campo("MontoGravadoI1", EcfCampoPresence.Prohibido, 90, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("MontoGravadoI2", EcfCampoPresence.Prohibido, 91, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("MontoExento", EcfCampoPresence.Prohibido, 92, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("ITBIS1", EcfCampoPresence.Prohibido, 93, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("ITBIS2", EcfCampoPresence.Prohibido, 94, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("TotalITBISRetenido", EcfCampoPresence.Prohibido, 95, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("TotalISRRetencion", EcfCampoPresence.Prohibido, 96, nota: "No en XSD E46"),
                EcfXmlFormat.Campo("MontoPropinaLegal", EcfCampoPresence.Prohibido, 99,
                    nota: "No existe en XSD Totales e-CF",
                    prohibidoModo: EcfProhibidoModo.Omitir),
            };
        }

        private static IReadOnlyList<EcfCampoDef> BuildItemE46() => new[]
        {
            EcfXmlFormat.Campo("NumeroLinea", EcfCampoPresence.Obligatorio, 1,
                c => c.LineaActual!.NumeroLinea),
            EcfXmlFormat.Campo("IndicadorFacturacion", EcfCampoPresence.Obligatorio, 2,
                c => 3,
                nota: "Formato DGII nota 51: tipo 46 → siempre 3 (ITBIS tasa cero)"),
            EcfXmlFormat.Campo("NombreItem", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.Esc(c.LineaActual!.NombreItem, 80)),
            EcfXmlFormat.Campo("IndicadorBienoServicio", EcfCampoPresence.Obligatorio, 4,
                c => c.LineaActual!.EsBien ? 1 : 2),
            EcfXmlFormat.Campo("DescripcionItem", EcfCampoPresence.Opcional, 5,
                c => string.IsNullOrWhiteSpace(c.LineaActual!.DescripcionItem)
                    ? null
                    : EcfXmlFormat.Esc(c.LineaActual.DescripcionItem, 1000)),
            EcfXmlFormat.Campo("CantidadItem", EcfCampoPresence.Obligatorio, 6,
                c => EcfXmlFormat.Money(c.LineaActual!.Cantidad)),
            EcfXmlFormat.Campo("UnidadMedida", EcfCampoPresence.Opcional, 7,
                c => c.LineaActual!.UnidadMedida),
            EcfXmlFormat.Campo("PrecioUnitarioItem", EcfCampoPresence.Obligatorio, 8,
                c => EcfXmlFormat.Money(c.LineaActual!.PrecioUnitario)),
            EcfXmlFormat.Campo("DescuentoMonto", EcfCampoPresence.Opcional, 9,
                c => c.LineaActual!.DescuentoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.DescuentoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("TablaSubDescuento", EcfCampoPresence.Opcional, 10,
                c => EcfXmlFormat.TablaSubDescuentoItem(c), complejo: true),
            EcfXmlFormat.Campo("RecargoMonto", EcfCampoPresence.Opcional, 11,
                c => c.LineaActual!.RecargoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.RecargoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("TablaSubRecargo", EcfCampoPresence.Opcional, 12,
                c => EcfXmlFormat.TablaSubRecargoItem(c), complejo: true),
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 13,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };
    }
}
