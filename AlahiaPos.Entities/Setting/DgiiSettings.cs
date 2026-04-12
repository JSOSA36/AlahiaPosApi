namespace AlahiaPos.Entities.Setting
{
    public class DgiiSettings
    {
        // Hosts
        public string HostEcf { get; set; } = "https://ecf.dgii.gov.do";
        public string HostFc { get; set; } = "https://fc.dgii.gov.do";

        // Servers (bases)
        public string BaseUrlAuth { get; set; } = "/CerteCF/Autenticacion";
        public string BaseUrlEcf { get; set; } = "/CerteCF/Recepcion";
        public string BaseUrlConsulta { get; set; } = "/CerteCF/ConsultaResultado";
        public string BaseUrlFc { get; set; } = "/CerteCF/recepcionfc"; // minúscula como tu OpenAPI

        // Auth endpoints
        public string SemillaEndpoint { get; set; } = "/api/Autenticacion/Semilla";
        public string ValidarSemillaEndpoint { get; set; } = "/api/Autenticacion/ValidarSemilla";

        // eCF endpoints
        public string RecepcionEcfEndpoint { get; set; } = "/api/FacturasElectronicas";

        // Consulta endpoints
        public string ConsultaEstadoEndpoint { get; set; } = "/api/Consultas/Estado"; // GET ?TrackId=

        // RFCE endpoints
        public string RecepcionRfceEndpoint { get; set; } = "/api/recepcion/ecf";

        // Certificado
        public string P12Path { get; set; }
        public string P12Password { get; set; }

        // Token fijo opcional
        public string? TokenFijo { get; set; }
    }
}