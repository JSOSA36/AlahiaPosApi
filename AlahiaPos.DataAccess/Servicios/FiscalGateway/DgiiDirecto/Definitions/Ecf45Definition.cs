namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Comprobante Gubernamental Electrónico (E45).</summary>
    public sealed class Ecf45Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 45;
        public override string Codigo => "E45";
        public override string Nombre => "Comprobante Gubernamental Electrónico";
        public override string XsdArchivo => "e-CF 45 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;

        public override IReadOnlyList<EcfCampoDef>? Comprador { get; } =
            BuildCompradorComun(forzarRncVacio: false, rncObligatorio: true);

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria.",
            "TipoIngresos y TipoPago obligatorios.",
            "IndicadorMontoGravado opcional; condicional si hay ítems gravados (misma regla práctica que E41).",
            "TablaFormasPago opcional.",
            "RNCComprador obligatorio (minOccurs=1) — entidad gubernamental.",
            "Totales con ITBIS/gravados (como E31); sin TotalITBISRetenido/TotalISRRetencion en XSD.",
            "IndicadorFacturacion NO forzado a 4 (a diferencia de 43/44/47).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        /// <summary>
        /// Totales comunes sin campos de retención (no existen en XSD E45).
        /// </summary>
        public override IReadOnlyList<EcfCampoDef> Totales { get; } = BuildTotalesE45();

        public Ecf45Definition()
        {
            var id = IdDocInicio(45);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(CampoIndicadorMontoGravado(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Obligatorio, 5));
            id.Add(CampoTipoPago(EcfCampoPresence.Obligatorio, 6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);

            if (string.IsNullOrWhiteSpace(ctx.Enc.RncComprador))
                throw new InvalidOperationException("E45: RNCComprador es obligatorio.");
        }

        private static IReadOnlyList<EcfCampoDef> BuildTotalesE45()
        {
            var list = new List<EcfCampoDef>(BuildTotalesComun());
            list.RemoveAll(c => c.Nombre is "TotalITBISRetenido" or "TotalISRRetencion");
            list.Add(EcfXmlFormat.Campo("TotalITBISRetenido", EcfCampoPresence.Prohibido, 14,
                nota: "No existe en XSD Totales E45"));
            list.Add(EcfXmlFormat.Campo("TotalISRRetencion", EcfCampoPresence.Prohibido, 15,
                nota: "No existe en XSD Totales E45"));
            return list;
        }
    }
}
