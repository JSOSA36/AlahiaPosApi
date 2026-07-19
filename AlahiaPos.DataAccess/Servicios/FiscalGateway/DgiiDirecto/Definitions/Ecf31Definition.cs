namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Factura de Crédito Fiscal Electrónica (E31).</summary>
    public sealed class Ecf31Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 31;
        public override string Codigo => "E31";
        public override string Nombre => "Factura de Crédito Fiscal Electrónica";
        public override string XsdArchivo => "e-CF 31 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => false;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia => null;

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "Aceptado en testecf (canal e-CF individual).",
            "FechaVencimientoSecuencia es obligatoria (XSD).",
            "TipoIngresos es obligatorio.",
            "TablaFormasPago es opcional en IdDoc.",
            "RNC comprador: si falta se emite 000000000.",
            "Sin namespace XML; UTF-8 sin BOM.",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf31Definition()
        {
            var id = IdDocInicio(31);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(CampoIndicadorMontoGravado(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Obligatorio, 5));
            id.Add(CampoTipoPago(6));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 7));
            IdDoc = id;
        }
    }
}
