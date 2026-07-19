namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Factura de Consumo Electrónica (E32). Puede ir por RFCE si MontoTotal &lt; 250000.</summary>
    public sealed class Ecf32Definition : EcfTipoDefinitionBase
    {
        public const decimal UmbralRfceMontoTotal = 250_000m;

        public override int TipoeCF => 32;
        public override string Codigo => "E32";
        public override string Nombre => "Factura de Consumo Electrónica";
        public override string XsdArchivo => "e-CF 32 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;
        public override IReadOnlyList<EcfCampoDef>? Comprador { get; } = BuildCompradorComun(forzarRncVacio: false);

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia NO existe en XSD E32.",
            "TipoIngresos obligatorio.",
            "TablaFormasPago opcional en IdDoc.",
            "RNC comprador opcional (no forzar 000000000) si MontoTotal < 250000.",
            "Si MontoTotal >= 250000 → RNCComprador obligatorio 9/11 dígitos válidos (no ceros).",
            "Si MontoTotal < 250000 → canal RFCE (fc.../recepcionfc); si ≥ 250000 → e-CF individual.",
            "Para RFCE: se firma e-CF 32 local, CodigoSeguridadeCF = primeros 6 de SignatureValue, luego RFCE.",
            "Aceptado en testecf tanto ≥250k (ECF) como <250k (RFCE).",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf32Definition()
        {
            var id = IdDocInicio(32);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Prohibido, 3));
            id.Add(CampoIndicadorMontoGravado(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Obligatorio, 5));
            id.Add(CampoTipoPago(6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }

        public override void Validar(EcfBuildContext ctx)
        {
            base.Validar(ctx);
            if (ctx.Enc.MontoTotal >= UmbralRfceMontoTotal)
                EcfRncRules.ExigirRncValido(ctx.Enc.RncComprador, Codigo, "RNCComprador");
        }
    }
}
