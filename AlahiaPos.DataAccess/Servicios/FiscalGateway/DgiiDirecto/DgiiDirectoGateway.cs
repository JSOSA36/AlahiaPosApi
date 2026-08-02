using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    /// <summary>
    /// Adaptador IFiscalGateway → protocolo oficial DGII (XML + semilla + recepción).
    /// </summary>
    public class DgiiDirectoGateway : IFiscalGateway
    {
        private readonly DgiiAuthService _auth;
        private readonly DgiiRecepcionClient _recepcion;
        private readonly DgiiXmlBuilder _xmlBuilder;
        private readonly DgiiRfceBuilder _rfceBuilder;
        private readonly DgiiCertificadoResolver _certs;
        private readonly DgiiDirectoSettings _settings;
        private readonly ILogger<DgiiDirectoGateway> _logger;

        public DgiiDirectoGateway(
            DgiiAuthService auth,
            DgiiRecepcionClient recepcion,
            DgiiXmlBuilder xmlBuilder,
            DgiiRfceBuilder rfceBuilder,
            DgiiCertificadoResolver certs,
            IOptions<DgiiDirectoSettings> settings,
            ILogger<DgiiDirectoGateway> logger)
        {
            _auth = auth;
            _recepcion = recepcion;
            _xmlBuilder = xmlBuilder;
            _rfceBuilder = rfceBuilder;
            _certs = certs;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<FiscalEnvioResultado> EnviarDocumentoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            using var _ = DgiiAmbienteContext.Push(documento.AmbienteDgii ?? _settings.Ambiente);
            var eff = _settings.Effective();
            try
            {
                var fechaFirma = DateTime.Now;
                var xmlSinFirmar = _xmlBuilder.Build(documento, fechaFirma);
                var material = await _certs.ResolveAsync(documento.IdEmpresa, ct);
                var xmlFirmado = _certs.Firmar(xmlSinFirmar, material);
                var nombre = DgiiXmlBuilder.NombreArchivo(
                    documento.Encabezado.RncEmisor,
                    documento.Encabezado.Encf);

                _logger.LogInformation(
                    "DgiiDirecto: enviando {Encf} tipo {Tipo} ambiente={Ambiente} cert={Cert}",
                    documento.Encabezado.Encf,
                    documento.Encabezado.TipoEcf,
                    eff.AmbientePath,
                    material.Source);

                var resp = await _recepcion.EnviarEcfAsync(xmlFirmado, nombre, documento.IdEmpresa, ct);
                var resultado = DgiiDirectoMapper.ToEnvioResultado(
                    resp, documento, xmlSinFirmar, xmlFirmado, fechaFirma, eff);

                _logger.LogInformation(
                    "DgiiDirecto: {Encf} Exitoso={Ok} TrackId={TrackId} Estado={Estado}",
                    documento.Encabezado.Encf, resultado.Exitoso, resultado.TrackId, resultado.Estado);

                return resultado;
            }
            catch (TaskCanceledException)
            {
                return FiscalEnvioResultado.Error("TIMEOUT", "Timeout de comunicación con DGII");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "DgiiDirecto HTTP error {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("HTTP_ERROR", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DgiiDirecto error {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("GATEWAY_ERROR", ex.Message);
            }
        }

        /// <summary>
        /// E32 &lt; RD$250,000: firma e-CF local → arma RFCE con CodigoSeguridad → firma RFCE → canal FC.
        /// </summary>
        public async Task<FiscalEnvioResultado> EnviarResumenConsumoAsync(
            FiscalDocumentoElectronico documento,
            CancellationToken ct = default)
        {
            using var _ = DgiiAmbienteContext.Push(documento.AmbienteDgii ?? _settings.Ambiente);
            var eff = _settings.Effective();
            try
            {
                if (documento.Encabezado.TipoEcf != 32)
                    return FiscalEnvioResultado.Error("RFCE_TIPO", "RFCE solo aplica a tipo 32.");
                if (documento.Encabezado.MontoTotal >= 250_000m)
                    return FiscalEnvioResultado.Error("RFCE_MONTO", "MontoTotal >= 250000 debe ir por canal e-CF individual.");

                var fechaFirma = DateTime.Now;
                var material = await _certs.ResolveAsync(documento.IdEmpresa, ct);

                // 1) e-CF completo local (no se envía) → se firma y se extrae código de seguridad
                var xmlEcf = _xmlBuilder.Build(documento, fechaFirma);
                var xmlEcfFirmado = _certs.Firmar(xmlEcf, material);

                // 2) RFCE se arma desde el e-CF firmado (mismo criterio que libs DGII oficiales)
                var rfce = _rfceBuilder.BuildFromSignedEcf(xmlEcfFirmado, out var codigo);
                var rfceFirmado = _certs.Firmar(rfce, material);
                var nombre = DgiiRfceBuilder.NombreArchivo(
                    documento.Encabezado.RncEmisor,
                    documento.Encabezado.Encf);

                _logger.LogInformation(
                    "DgiiDirecto RFCE: {Encf} monto={Monto} ambiente={Ambiente} codigo={Codigo}",
                    documento.Encabezado.Encf, documento.Encabezado.MontoTotal, eff.AmbientePath, codigo);

                var resp = await _recepcion.EnviarRfceAsync(rfceFirmado, nombre, documento.IdEmpresa, ct);
                var resultado = DgiiDirectoMapper.ToEnvioResultado(
                    resp, documento, rfce, rfceFirmado, fechaFirma, eff);
                resultado.SecurityCode = codigo;
                resultado.UrlQR = DgiiDirectoMapper.BuildQrUrl(documento, fechaFirma, codigo, eff);
                // Conservar e-CF local firmado en debug (el que no se envía a DGII).
                if (!resultado.Exitoso)
                    resultado.XmlSinFirmar = xmlEcfFirmado;

                _logger.LogInformation(
                    "DgiiDirecto RFCE: {Encf} Exitoso={Ok} TrackId={TrackId} Estado={Estado} Codigo={Codigo}",
                    documento.Encabezado.Encf, resultado.Exitoso, resultado.TrackId, resultado.Estado, codigo);

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DgiiDirecto RFCE error {Encf}", documento.Encabezado.Encf);
                return FiscalEnvioResultado.Error("GATEWAY_ERROR", ex.Message);
            }
        }

        public async Task<FiscalConsultaResultado> ConsultarEstadoAsync(
            string trackId,
            CancellationToken ct = default)
        {
            try
            {
                // Consulta sin IdEmpresa específico: usa certificado fallback de config (idEmpresa=0)
                var resp = await _recepcion.ConsultarEstadoAsync(trackId, 0, ct);
                return DgiiDirectoMapper.ToConsultaResultado(resp, trackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DgiiDirecto consulta TrackId={TrackId}", trackId);
                return new FiscalConsultaResultado
                {
                    TrackId = trackId,
                    Estado = "Error",
                    CodigoError = "GATEWAY_ERROR",
                    Mensajes = new List<string> { ex.Message }
                };
            }
        }

        public Task<bool> VerificarConexionAsync(CancellationToken ct = default)
            => _auth.VerificarConexionAsync(ct);
    }
}
