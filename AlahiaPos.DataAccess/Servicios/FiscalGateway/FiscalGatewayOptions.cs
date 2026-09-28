namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Configuración del Gateway Fiscal vista por el ERP.
    /// El ERP no conoce el proveedor: solo URL, credenciales y timeout.
    /// Cambiar de PG → Alahia.eCF.Api → otro compatible = actualizar estos valores.
    /// </summary>
    public class FiscalGatewayOptions
    {
        public const string SectionName = "FiscalGateway";

        /// <summary>Base URL del proveedor e-CF (ej. https://ecf.alahiapos.com o http://localhost:5203).</summary>
        public string BaseUrl { get; set; } = "";

        /// <summary>
        /// Host HTTPS que DGII debe llamar (CerteCF recepción).
        /// Si BaseUrl es localhost, las URLs de postulación usan este valor.
        /// </summary>
        public string PublicBaseUrl { get; set; } = "";

        /// <summary>API Key / token del proveedor.</summary>
        public string ApiKey { get; set; } = "";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
