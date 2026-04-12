namespace Alahia.eCF.Api.Setting
{
    public class DgiiSettings
    {

        public bool UseTestEnvironment { get; set; }

        public string HostEcf { get; set; }
        public string BaseUrlAuth { get; set; }
        public string BaseUrlEcf { get; set; }
        public string BaseUrlConsulta { get; set; }

        public string SemillaEndpoint { get; set; }
        public string ValidarSemillaEndpoint { get; set; }

        public string RecepcionEcfEndpoint { get; set; }
        public string ConsultaEstadoEndpoint { get; set; }
    }
}
