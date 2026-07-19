namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Nota de Crédito Electrónica (E34).</summary>
    public sealed class Ecf34Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 34;
        public override string Codigo => "E34";
        public override string Nombre => "Nota de Crédito Electrónica";
        public override string XsdArchivo => "e-CF 34 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => true;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia { get; } = BuildReferenciaComun();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "IndicadorNotaCredito obligatorio: 0 si emisión ≤30 días del NCF modificado; 1 si >30 días.",
            "FechaVencimientoSecuencia NO existe en XSD E34.",
            "TablaFormasPago PROHIBIDA en IdDoc: DGII rechaza con error XSD si se incluye (hallazgo testecf).",
            "TipoIngresos opcional en XSD (se emite igual).",
            "InformacionReferencia obligatoria (NCFModificado, FechaNCFModificado, CodigoModificacion).",
            "Si el DTO trae FormasPago, el motor las omite (no las emite).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf34Definition()
        {
            var id = IdDocInicio(34);
            id.Add(EcfXmlFormat.Campo("IndicadorNotaCredito", EcfCampoPresence.Obligatorio, 3,
                c =>
                {
                    var ind = c.Enc.IndicadorNotaCredito ?? 0;
                    if (ind is not (0 or 1))
                        throw new InvalidOperationException("E34: IndicadorNotaCredito debe ser 0 o 1.");
                    return ind;
                },
                nota: "0 ≤30 días; 1 >30 días respecto al NCF modificado"));
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Prohibido, 4));
            id.Add(CampoIndicadorMontoGravado(5));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Opcional, 6));
            id.Add(CampoTipoPago(7));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Prohibido, 8));
            IdDoc = id;
        }
    }
}
