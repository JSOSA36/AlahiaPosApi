namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Regímenes Especiales Electrónico (E44).</summary>
    public sealed class Ecf44Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 44;
        public override string Codigo => "E44";
        public override string Nombre => "Regímenes Especiales Electrónico";
        public override string XsdArchivo => "e-CF 44 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;

        public override IReadOnlyList<EcfCampoDef>? Comprador { get; } =
            BuildCompradorComun(forzarRncVacio: false);

        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE44();
        public override IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemE44();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "TipoIngresos y TipoPago obligatorios (XSD).",
            "IndicadorMontoGravado NO existe en XSD E44.",
            "TablaFormasPago opcional.",
            "Comprador obligatorio; RNCComprador opcional.",
            "Totales: MontoExento + MontoTotal (sin ITBIS/gravados).",
            "IndicadorFacturacion debe ser 4 (Exento) — Formato DGII nota 50.",
            "DescuentosORecargos permitidos en XSD.",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf44Definition()
        {
            var id = IdDocInicio(44);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(EcfXmlFormat.Campo("IndicadorMontoGravado", EcfCampoPresence.Prohibido, 4,
                nota: "No existe en XSD E44"));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Obligatorio, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Obligatorio, 6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            foreach (var l in ctx.Documento.Lineas)
            {
                if (l.IndicadorFacturacion != 4)
                    throw new InvalidOperationException(
                        $"E44: Item {l.NumeroLinea} IndicadorFacturacion debe ser 4 (Exento).");
            }

            if (ctx.Enc.MontoTotal <= 0)
                throw new InvalidOperationException("E44: MontoTotal debe ser > 0.");
        }

        private static IReadOnlyList<EcfCampoDef> BuildTotalesE44() => new[]
        {
            EcfXmlFormat.Campo("MontoExento", EcfCampoPresence.Opcional, 1,
                c => c.Enc.MontoExento > 0
                    ? EcfXmlFormat.Money(c.Enc.MontoExento)
                    : (c.Enc.MontoTotal > 0 ? EcfXmlFormat.Money(c.Enc.MontoTotal) : null),
                nota: "E44: montos exentos; si no viene MontoExento se usa MontoTotal"),
            EcfXmlFormat.Campo("MontoTotal", EcfCampoPresence.Obligatorio, 2,
                c => EcfXmlFormat.Money(c.Enc.MontoTotal)),
            EcfXmlFormat.Campo("MontoPropinaLegal", EcfCampoPresence.Prohibido, 99,
                nota: "No existe en XSD Totales e-CF; no emitir",
                prohibidoModo: EcfProhibidoModo.Omitir),
        };

        private static IReadOnlyList<EcfCampoDef> BuildItemE44() => new[]
        {
            EcfXmlFormat.Campo("NumeroLinea", EcfCampoPresence.Obligatorio, 1,
                c => c.LineaActual!.NumeroLinea),
            EcfXmlFormat.Campo("IndicadorFacturacion", EcfCampoPresence.Obligatorio, 2,
                c => 4,
                nota: "Formato DGII: tipos 43/44/47 → siempre 4 (Exento)"),
            EcfXmlFormat.Campo("NombreItem", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.Esc(c.LineaActual!.NombreItem, 80)),
            EcfXmlFormat.Campo("IndicadorBienoServicio", EcfCampoPresence.Obligatorio, 4,
                c => c.LineaActual!.EsBien ? 1 : 2),
            EcfXmlFormat.Campo("CantidadItem", EcfCampoPresence.Obligatorio, 5,
                c => EcfXmlFormat.Money(c.LineaActual!.Cantidad)),
            EcfXmlFormat.Campo("UnidadMedida", EcfCampoPresence.Opcional, 6,
                c => c.LineaActual!.UnidadMedida),
            EcfXmlFormat.Campo("PrecioUnitarioItem", EcfCampoPresence.Obligatorio, 7,
                c => EcfXmlFormat.Money(c.LineaActual!.PrecioUnitario)),
            EcfXmlFormat.Campo("DescuentoMonto", EcfCampoPresence.Opcional, 8,
                c => c.LineaActual!.DescuentoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.DescuentoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("RecargoMonto", EcfCampoPresence.Opcional, 9,
                c => c.LineaActual!.RecargoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.RecargoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 10,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };
    }
}
