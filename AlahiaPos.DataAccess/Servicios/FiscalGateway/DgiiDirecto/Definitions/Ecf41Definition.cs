namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Comprobante de Compras Electrónico (E41).</summary>
    public sealed class Ecf41Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 41;
        public override string Codigo => "E41";
        public override string Nombre => "Comprobante de Compras Electrónico";
        public override string XsdArchivo => "e-CF 41 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;
        public override IReadOnlyList<EcfCampoDef>? Comprador { get; } =
            BuildCompradorComun(forzarRncVacio: false, rncObligatorio: true, incluirDatosOrden: false);
        public override IReadOnlyList<EcfCampoDef> Item { get; } = BuildItemE41();
        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE41();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "TipoIngresos NO existe en XSD E41 (prohibido).",
            "TipoPago opcional en XSD; TablaFormasPago opcional.",
            "RNCComprador obligatorio (minOccurs=1): contraparte/proveedor.",
            "Cada Item exige nodo Retencion (minOccurs=1) antes de NombreItem.",
            "Regla DGII: e-CF 41 condicional a retención e IndicadorBienoServicio=2 (servicio).",
            "MontoISRRetenido / TotalISRRetencion: típico en prestación de servicios.",
            "Hallazgo testecf: con ítems gravados, IndicadorMontoGravado es obligatorio (0/1); sin él DGII rechaza.",
            "Hallazgo testecf: MontoITBISRetenido y TotalITBISRetenido deben emitirse (pueden ser 0 si solo se retiene ISR).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf41Definition()
        {
            var id = IdDocInicio(41);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(CampoIndicadorMontoGravado(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Prohibido, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Opcional, 6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            if (string.IsNullOrWhiteSpace(ctx.Enc.RncComprador))
                throw new InvalidOperationException("E41: RNCComprador es obligatorio.");

            if (ctx.Enc.TotalIsrRetencion <= 0 && ctx.Enc.TotalItbisRetenido <= 0)
                throw new InvalidOperationException(
                    "E41: requiere retención (TotalISRRetencion y/o TotalITBISRetenido > 0).");

            foreach (var l in ctx.Documento.Lineas)
            {
                if (l.EsBien)
                    throw new InvalidOperationException(
                        "E41: IndicadorBienoServicio debe ser 2 (servicio) en todos los ítems.");

                var isr = l.MontoIsrRetenido ?? 0m;
                var itbis = l.MontoItbisRetenido ?? 0m;
                if (isr <= 0 && itbis <= 0)
                    throw new InvalidOperationException(
                        $"E41: Item {l.NumeroLinea} requiere Retencion con MontoISRRetenido y/o MontoITBISRetenido > 0.");

                var ind = l.IndicadorAgenteRetencionoPercepcion ?? 1;
                if (ind is not (1 or 2))
                    throw new InvalidOperationException(
                        $"E41: Item {l.NumeroLinea} IndicadorAgenteRetencionoPercepcion debe ser 1 o 2.");
            }
        }

        /// <summary>
        /// Orden XSD E41: NumeroLinea → IndicadorFacturacion → Retencion → NombreItem → …
        /// </summary>
        private static IReadOnlyList<EcfCampoDef> BuildItemE41() => new[]
        {
            EcfXmlFormat.Campo("NumeroLinea", EcfCampoPresence.Obligatorio, 1,
                c => c.LineaActual!.NumeroLinea),
            EcfXmlFormat.Campo("IndicadorFacturacion", EcfCampoPresence.Obligatorio, 2,
                c => c.LineaActual!.IndicadorFacturacion),
            EcfXmlFormat.Campo("Retencion", EcfCampoPresence.Obligatorio, 3,
                c => EcfXmlFormat.RetencionItem(c, emitirMontosAunqueCero: true),
                complejo: true,
                nota: "XSD E41: Retencion minOccurs=1; emitir MontoITBISRetenido aunque sea 0"),
            EcfXmlFormat.Campo("NombreItem", EcfCampoPresence.Obligatorio, 4,
                c => EcfXmlFormat.Esc(c.LineaActual!.NombreItem, 80)),
            EcfXmlFormat.Campo("IndicadorBienoServicio", EcfCampoPresence.Obligatorio, 5,
                c => 2,
                nota: "E41: debe ser 2 (servicio) según Formato DGII"),
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
            EcfXmlFormat.Campo("DescuentoMonto", EcfCampoPresence.Opcional, 10,
                c => c.LineaActual!.DescuentoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.DescuentoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("TablaSubDescuento", EcfCampoPresence.Opcional, 11,
                c => EcfXmlFormat.TablaSubDescuentoItem(c), complejo: true,
                nota: "CerteCF: DGII rechaza DescuentoMonto sin TablaSubDescuento"),
            EcfXmlFormat.Campo("RecargoMonto", EcfCampoPresence.Opcional, 12,
                c => c.LineaActual!.RecargoMonto is > 0
                    ? EcfXmlFormat.Money(c.LineaActual.RecargoMonto.Value)
                    : null),
            EcfXmlFormat.Campo("TablaSubRecargo", EcfCampoPresence.Opcional, 13,
                c => EcfXmlFormat.TablaSubRecargoItem(c), complejo: true),
            EcfXmlFormat.Campo("MontoItem", EcfCampoPresence.Obligatorio, 14,
                c => EcfXmlFormat.Money(c.LineaActual!.MontoItem)),
        };

        /// <summary>
        /// Igual a Totales comunes, pero TotalITBISRetenido / TotalISRRetencion se emiten
        /// aunque sean 0 (coherencia con Retencion por ítem exigida por DGII).
        /// </summary>
        private static IReadOnlyList<EcfCampoDef> BuildTotalesE41()
        {
            var list = new List<EcfCampoDef>(BuildTotalesComun());
            list.RemoveAll(c => c.Nombre is "TotalITBISRetenido" or "TotalISRRetencion");
            list.Add(EcfXmlFormat.Campo("TotalITBISRetenido", EcfCampoPresence.Obligatorio, 21,
                c => EcfXmlFormat.Money(c.Enc.TotalItbisRetenido),
                nota: "E41: emitir aunque sea 0 si hay Retencion en ítems"));
            list.Add(EcfXmlFormat.Campo("TotalISRRetencion", EcfCampoPresence.Obligatorio, 22,
                c => EcfXmlFormat.Money(c.Enc.TotalIsrRetencion),
                nota: "E41: retención ISR típica en servicios"));
            return list;
        }
    }
}
