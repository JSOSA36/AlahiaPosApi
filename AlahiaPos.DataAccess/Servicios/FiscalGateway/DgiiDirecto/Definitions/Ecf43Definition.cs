namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Gastos Menores Electrónico (E43).</summary>
    public sealed class Ecf43Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 43;
        public override string Codigo => "E43";
        public override string Nombre => "Gastos Menores Electrónico";
        public override string XsdArchivo => "e-CF 43 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;
        public override bool PermiteDescuentosORecargos => false;

        /// <summary>XSD E43: no existe sección Comprador.</summary>
        public override IReadOnlyList<EcfCampoDef>? Comprador => null;

        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE43();
        public override IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemE43();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "Sin TipoIngresos, IndicadorMontoGravado ni TablaFormasPago (XSD).",
            "Sin sección Comprador (XSD): Emisor → Totales.",
            "Totales: solo MontoExento (opc.) + MontoTotal; sin ITBIS/gravados/retención.",
            "IndicadorFacturacion debe ser 4 (Exento) — Formato DGII nota 50.",
            "Sin DescuentosORecargos en XSD E43.",
            "Montos de gastos menores no sirven como adelanto de ITBIS (Formato).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf43Definition()
        {
            var id = IdDocInicio(43);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(CampoIndicadorMontoGravadoProhibido(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Prohibido, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Opcional, 6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Prohibido, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            foreach (var l in ctx.Documento.Lineas)
            {
                if (l.IndicadorFacturacion != 4)
                    throw new InvalidOperationException(
                        $"E43: Item {l.NumeroLinea} IndicadorFacturacion debe ser 4 (Exento).");
            }

            if (ctx.Enc.MontoTotal <= 0)
                throw new InvalidOperationException("E43: MontoTotal debe ser > 0.");
        }

        private static EcfCampoDef CampoIndicadorMontoGravadoProhibido(int orden)
            => EcfXmlFormat.Campo("IndicadorMontoGravado", EcfCampoPresence.Prohibido, orden,
                nota: "No existe en XSD E43");

        private static IReadOnlyList<EcfCampoDef> BuildTotalesE43() => new[]
        {
            EcfXmlFormat.Campo("MontoExento", EcfCampoPresence.Opcional, 1,
                c => c.Enc.MontoExento > 0
                    ? EcfXmlFormat.Money(c.Enc.MontoExento)
                    : (c.Enc.MontoTotal > 0 ? EcfXmlFormat.Money(c.Enc.MontoTotal) : null),
                nota: "E43: montos son exentos; si no viene MontoExento se usa MontoTotal"),
            EcfXmlFormat.Campo("MontoTotal", EcfCampoPresence.Obligatorio, 2,
                c => EcfXmlFormat.Money(c.Enc.MontoTotal)),
            EcfXmlFormat.Campo("MontoPeriodo", EcfCampoPresence.Opcional, 3,
                c => c.Enc.MontoPeriodo is decimal mp ? EcfXmlFormat.Money(mp) : null),
            EcfXmlFormat.Campo("SaldoAnterior", EcfCampoPresence.Opcional, 4,
                c => c.Enc.SaldoAnterior is decimal sa ? EcfXmlFormat.Money(sa) : null),
            EcfXmlFormat.Campo("MontoAvancePago", EcfCampoPresence.Opcional, 5,
                c => c.Enc.MontoAvancePago is decimal ap ? EcfXmlFormat.Money(ap) : null),
            EcfXmlFormat.Campo("ValorPagar", EcfCampoPresence.Opcional, 6,
                c => c.Enc.ValorPagar is decimal vp ? EcfXmlFormat.Money(vp) : null),
            EcfXmlFormat.Campo("MontoPropinaLegal", EcfCampoPresence.Prohibido, 99,
                nota: "No existe en XSD Totales e-CF; no emitir",
                prohibidoModo: EcfProhibidoModo.Omitir),
        };

        private static IReadOnlyList<EcfCampoDef> BuildItemE43() => new[]
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
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 9,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };
    }
}
