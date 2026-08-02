namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Normaliza y describe los 3 ambientes oficiales DGII e-CF.
    /// </summary>
    public static class DgiiAmbienteHelper
    {
        public const string Pruebas = "testecf";
        public const string Certificacion = "certecf";
        public const string Produccion = "ecf";

        public static string Normalize(string? ambiente)
        {
            var a = (ambiente ?? "").Trim().ToLowerInvariant();
            return a switch
            {
                "cert" or "certecf" or "certificacion" or "certificación" or "certification" => Certificacion,
                "prod" or "ecf" or "produccion" or "producción" or "production" => Produccion,
                "test" or "testecf" or "pruebas" or "sandbox" or "prueba" => Pruebas,
                _ when a.Contains("cert") => Certificacion,
                _ when a.Contains("prod") => Produccion,
                _ => Pruebas
            };
        }

        public static string EtiquetaUi(string? ambiente) => Normalize(ambiente) switch
        {
            Certificacion => "Certificación",
            Produccion => "Producción",
            _ => "Pruebas"
        };

        /// <summary>Valor amigable para SecuenciaECF.Ambiente.</summary>
        public static string EtiquetaSecuencia(string? ambiente) => Normalize(ambiente) switch
        {
            Certificacion => "CERTIFICACION",
            Produccion => "PRODUCCION",
            _ => "PRUEBAS"
        };

        public static bool EsValido(string? ambiente)
        {
            var n = (ambiente ?? "").Trim().ToLowerInvariant();
            return n is Pruebas or Certificacion or Produccion
                or "cert" or "certecf" or "certificacion" or "certificación"
                or "prod" or "ecf" or "produccion" or "producción" or "production"
                or "test" or "testecf" or "pruebas" or "sandbox" or "prueba";
        }

        public static object BuildUrlsDto(DgiiDirectoSettings settings)
        {
            var s = settings.WithAmbiente(settings.Ambiente);
            return new
            {
                auth = s.AuthBaseUrl,
                recepcion = s.RecepcionBaseUrl,
                consulta = s.ConsultaBaseUrl,
                rfce = s.RecepcionFcBaseUrl,
                hostEcf = s.HostEcf,
                hostFc = s.HostFc,
                ambientePath = s.AmbientePath
            };
        }
    }
}
