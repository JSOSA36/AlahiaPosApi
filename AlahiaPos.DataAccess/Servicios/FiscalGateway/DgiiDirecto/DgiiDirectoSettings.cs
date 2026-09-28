namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Configuración del adaptador DGII directo (protocolo oficial e-CF).
    /// </summary>
    public class DgiiDirectoSettings
    {
        /// <summary>testecf | certecf | ecf</summary>
        public string Ambiente { get; set; } = "testecf";

        public string HostEcf { get; set; } = "https://ecf.dgii.gov.do";
        public string HostFc { get; set; } = "https://fc.dgii.gov.do";

        public string SemillaEndpoint { get; set; } = "/api/autenticacion/semilla";
        public string ValidarSemillaEndpoint { get; set; } = "/api/autenticacion/validarsemilla";
        public string RecepcionEcfEndpoint { get; set; } = "/api/facturaselectronicas";
        public string ConsultaEstadoEndpoint { get; set; } = "/api/consultas/estado";

        /// <summary>Fallback si no hay CertificadoDigital activo en BD.</summary>
        public string? P12Path { get; set; }
        public string? P12Password { get; set; }

        /// <summary>Si true, usa P12 de appsettings antes que CertificadoDigital en BD.</summary>
        public bool PreferSettingsCertificate { get; set; }

        public string? TokenFijo { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        public string AmbientePath
        {
            get
            {
                var a = (Ambiente ?? "testecf").Trim().ToLowerInvariant();
                return a switch
                {
                    "cert" or "certecf" or "certificacion" => "certecf",
                    "prod" or "ecf" or "produccion" or "production" => "ecf",
                    _ => "testecf"
                };
            }
        }

        public string AuthBaseUrl => $"{HostEcf.TrimEnd('/')}/{AmbientePath}/autenticacion";
        public string RecepcionBaseUrl => $"{HostEcf.TrimEnd('/')}/{AmbientePath}/recepcion";
        public string ConsultaBaseUrl => $"{HostEcf.TrimEnd('/')}/{AmbientePath}/consultaresultado";
        public string ConsultaTimbreBaseUrl => $"{HostEcf.TrimEnd('/')}/{AmbientePath}/ConsultaTimbre";
        public string RecepcionFcBaseUrl => $"{HostFc.TrimEnd('/')}/{AmbientePath}/recepcionfc";
        public string RecepcionRfceEndpoint { get; set; } = "/api/recepcion/ecf";
        public string AprobacionComercialEndpoint { get; set; } = "/api/aprobacioncomercial";

        public string AprobacionComercialBaseUrl => $"{HostEcf.TrimEnd('/')}/{AmbientePath}/aprobacioncomercial";

        /// <summary>Copia con ambiente normalizado (no muta la instancia de DI).</summary>
        public DgiiDirectoSettings WithAmbiente(string? ambiente)
        {
            return new DgiiDirectoSettings
            {
                Ambiente = DgiiAmbienteHelper.Normalize(ambiente ?? Ambiente),
                HostEcf = HostEcf,
                HostFc = HostFc,
                SemillaEndpoint = SemillaEndpoint,
                ValidarSemillaEndpoint = ValidarSemillaEndpoint,
                RecepcionEcfEndpoint = RecepcionEcfEndpoint,
                ConsultaEstadoEndpoint = ConsultaEstadoEndpoint,
                P12Path = P12Path,
                P12Password = P12Password,
                PreferSettingsCertificate = PreferSettingsCertificate,
                TokenFijo = TokenFijo,
                TimeoutSeconds = TimeoutSeconds,
                RecepcionRfceEndpoint = RecepcionRfceEndpoint,
                AprobacionComercialEndpoint = AprobacionComercialEndpoint
            };
        }

        /// <summary>Settings efectivos: AsyncLocal si hay, si no appsettings.</summary>
        public DgiiDirectoSettings Effective()
            => WithAmbiente(DgiiAmbienteContext.Current ?? Ambiente);
    }
}
