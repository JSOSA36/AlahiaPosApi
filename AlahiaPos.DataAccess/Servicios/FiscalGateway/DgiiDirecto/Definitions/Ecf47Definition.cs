namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Comprobante para Pagos al Exterior Electrónico (E47).</summary>
    public sealed class Ecf47Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 47;
        public override string Codigo => "E47";
        public override string Nombre => "Comprobante para Pagos al Exterior Electrónico";
        public override string XsdArchivo => "e-CF 47 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;
        public override bool PermiteDescuentosORecargos => false;

        /// <summary>XSD: Comprador opcional y sin RNC (solo IdentificadorExtranjero / Razón). Happy path: omitir.</summary>
        public override IReadOnlyList<EcfCampoDef>? Comprador => null;

        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE47();
        public override IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemE47();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "Sin TipoIngresos ni IndicadorMontoGravado (XSD).",
            "TipoPago y TablaFormasPago opcionales.",
            "Comprador opcional; si existe no lleva RNCComprador (solo IdentificadorExtranjero / Razón).",
            "Totales: MontoExento + MontoTotal + TotalISRRetencion; sin ITBIS/gravados.",
            "IndicadorFacturacion = 4 (Exento) — Formato nota 50.",
            "Cada Item exige Retencion con MontoISRRetenido obligatorio (sin MontoITBISRetenido en XSD E47).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf47Definition()
        {
            var id = IdDocInicio(47);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(EcfXmlFormat.Campo("IndicadorMontoGravado", EcfCampoPresence.Prohibido, 4,
                nota: "No existe en XSD E47"));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Prohibido, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Opcional, 6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            if (ctx.Enc.MontoTotal <= 0)
                throw new InvalidOperationException("E47: MontoTotal debe ser > 0.");

            foreach (var l in ctx.Documento.Lineas)
            {
                if (l.IndicadorFacturacion != 4)
                    throw new InvalidOperationException(
                        $"E47: Item {l.NumeroLinea} IndicadorFacturacion debe ser 4 (Exento).");

                if ((l.MontoIsrRetenido ?? 0m) < 0)
                    throw new InvalidOperationException(
                        $"E47: Item {l.NumeroLinea} MontoISRRetenido es obligatorio (≥ 0).");
            }
        }

        private static IReadOnlyList<EcfCampoDef> BuildTotalesE47() => new[]
        {
            EcfXmlFormat.Campo("MontoExento", EcfCampoPresence.Opcional, 1,
                c => c.Enc.MontoExento > 0
                    ? EcfXmlFormat.Money(c.Enc.MontoExento)
                    : (c.Enc.MontoTotal > 0 ? EcfXmlFormat.Money(c.Enc.MontoTotal) : null)),
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
            EcfXmlFormat.Campo("TotalISRRetencion", EcfCampoPresence.Opcional, 7,
                c => EcfXmlFormat.Money(c.Enc.TotalIsrRetencion),
                nota: "Emitir inclusive 0 si hay Retencion en ítems"),
            EcfXmlFormat.Campo("MontoPropinaLegal", EcfCampoPresence.Prohibido, 99,
                nota: "No existe en XSD Totales e-CF",
                prohibidoModo: EcfProhibidoModo.Omitir),
        };

        private static IReadOnlyList<EcfCampoDef> BuildItemE47() => new[]
        {
            EcfXmlFormat.Campo("NumeroLinea", EcfCampoPresence.Obligatorio, 1,
                c => c.LineaActual!.NumeroLinea),
            EcfXmlFormat.Campo("IndicadorFacturacion", EcfCampoPresence.Obligatorio, 2,
                c => 4,
                nota: "Formato DGII: tipos 43/44/47 → siempre 4 (Exento)"),
            EcfXmlFormat.Campo("Retencion", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.RetencionItem(c, soloIsr: true),
                complejo: true,
                nota: "XSD E47: MontoISRRetenido minOccurs=1; sin MontoITBISRetenido"),
            EcfXmlFormat.Campo("NombreItem", EcfCampoPresence.Obligatorio, 4,
                c => EcfXmlFormat.Esc(c.LineaActual!.NombreItem, 80)),
            EcfXmlFormat.Campo("IndicadorBienoServicio", EcfCampoPresence.Obligatorio, 5,
                c => c.LineaActual!.EsBien ? 1 : 2),
            EcfXmlFormat.Campo("DescripcionItem", EcfCampoPresence.Opcional, 6,
                c => string.IsNullOrWhiteSpace(c.LineaActual!.DescripcionItem)
                    ? null
                    : EcfXmlFormat.Esc(c.LineaActual.DescripcionItem, 1000)),
            EcfXmlFormat.Campo("CantidadItem", EcfCampoPresence.Obligatorio, 7,
                c => EcfXmlFormat.Money(c.LineaActual!.Cantidad)),
            EcfXmlFormat.Campo("UnidadMedida", EcfCampoPresence.Opcional, 8,
                c => c.LineaActual!.UnidadMedida),
            EcfXmlFormat.Campo("PrecioUnitarioItem", EcfCampoPresence.Obligatorio, 9,
                c => EcfXmlFormat.Money(c.LineaActual!.PrecioUnitario)),
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 10,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };
    }
}
