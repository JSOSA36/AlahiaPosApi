namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Nota de Débito Electrónica (E33).</summary>
    public sealed class Ecf33Definition : EcfTipoDefinitionBase
    {
        public override int TipoeCF => 33;
        public override string Codigo => "E33";
        public override string Nombre => "Nota de Débito Electrónica";
        public override string XsdArchivo => "e-CF 33 v.1.0.xsd";
        public override bool RequiereInformacionReferencia => true;
        public override IReadOnlyList<EcfCampoDef>? InformacionReferencia { get; } = BuildReferenciaComun();

        public override IReadOnlyList<string> ConocimientoAcumulado { get; } = new[]
        {
            "FechaVencimientoSecuencia obligatoria (XSD).",
            "TipoIngresos opcional en XSD (se emite igual).",
            "TablaFormasPago permitida (a diferencia de E34).",
            "InformacionReferencia obligatoria (NCFModificado, FechaNCFModificado, CodigoModificacion).",
            "Aceptada en testecf referenciando un E31 previamente aceptado.",
        };

        public override IReadOnlyList<EcfCampoDef> IdDoc { get; }

        public Ecf33Definition()
        {
            var id = IdDocInicio(33);
            id.Add(CampoFechaVencimiento(EcfCampoPresence.Obligatorio, 3));
            id.Add(CampoIndicadorMontoGravado(4));
            id.Add(CampoTipoIngresos(EcfCampoPresence.Opcional, 5));
            id.Add(CampoTipoPago(6));
            id.Add(CampoFechaLimitePago(7));
            id.Add(CampoTerminoPago(8));
            id.Add(CampoTablaFormasPago(EcfCampoPresence.Opcional, 9));
            id.Add(CampoTipoCuentaPago(10));
            id.Add(CampoNumeroCuentaPago(11));
            id.Add(CampoBancoPago(12));
            IdDoc = id;
        }
    }
}
