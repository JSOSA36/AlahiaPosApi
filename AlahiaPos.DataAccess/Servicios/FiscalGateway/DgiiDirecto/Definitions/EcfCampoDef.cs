using AlahiaPos.Entities.Dto.Fiscal;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto.Definitions
{
    /// <summary>Contexto de construcción/validación para un e-CF.</summary>
    public sealed class EcfBuildContext
    {
        public FiscalDocumentoElectronico Documento { get; set; } = null!;
        public IEcfTipoDefinition Definicion { get; set; } = null!;
        public DateTime FechaHoraFirma { get; set; }
        public FiscalDocumentoEncabezado Enc => Documento.Encabezado;
        /// <summary>Línea actual al construir DetallesItems/Item.</summary>
        public FiscalDocumentoLinea? LineaActual { get; set; }
    }

    /// <summary>Definición de un campo/nodo XML en orden XSD.</summary>
    public sealed class EcfCampoDef
    {
        public string Nombre { get; set; } = "";
        public EcfCampoPresence Presence { get; set; }
        public int Orden { get; set; }
        /// <summary>Resuelve el valor textual/numérico del nodo. null = no emitir.</summary>
        public Func<EcfBuildContext, object?>? Resolver { get; set; }
        /// <summary>Validación puntual del campo (además de Presence).</summary>
        public Action<EcfBuildContext>? Validar { get; set; }
        public EcfProhibidoModo ProhibidoModo { get; set; } = EcfProhibidoModo.Omitir;
        /// <summary>Conocimiento / nota de negocio o XSD.</summary>
        public string? Nota { get; set; }
        /// <summary>Si true, el valor se emite como hijo complejo (el Resolver debe devolver XElement).</summary>
        public bool EsComplejo { get; set; }
    }
}
