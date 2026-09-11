using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Dto.Fiscal;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.FacturacionElectronica
{
    /// <summary>
    /// Envío ACECF (paso 3 CerteCF). No pasa por el pipeline certificado de e-CF.
    /// Reutiliza la misma firma P12 y el mismo cliente HTTP DGII.
    /// </summary>
    public interface ICertecfAcecfSender
    {
        Task<FiscalEnvioResultado> EnviarAsync(AcecfDocumento documento, CancellationToken ct = default);
    }

    public sealed class CertecfAcecfDirectSender : ICertecfAcecfSender
    {
        private readonly DgiiCertificadoResolver _certs;
        private readonly DgiiRecepcionClient _recepcion;
        private readonly ILogger<CertecfAcecfDirectSender> _logger;

        public CertecfAcecfDirectSender(
            DgiiCertificadoResolver certs,
            DgiiRecepcionClient recepcion,
            ILogger<CertecfAcecfDirectSender> logger)
        {
            _certs = certs;
            _recepcion = recepcion;
            _logger = logger;
        }

        public async Task<FiscalEnvioResultado> EnviarAsync(AcecfDocumento documento, CancellationToken ct = default)
        {
            using var _ = DgiiAmbienteContext.Push("certecf");
            try
            {
                var xml = AcecfXmlBuilder.Build(documento);
                var material = await _certs.ResolveAsync(documento.IdEmpresa, ct);
                var xmlFirmado = _certs.Firmar(xml, material);
                var nombre = AcecfXmlBuilder.NombreArchivo(documento.RncComprador, documento.Encf);
                var resp = await _recepcion.EnviarAcecfAsync(xmlFirmado, nombre, documento.IdEmpresa, ct);
                var codigo = resp.Codigo ?? "";
                var ok = resp.Ok && codigo != "2";
                if (codigo == "1") ok = true;
                return new FiscalEnvioResultado
                {
                    Exitoso = ok,
                    Estado = ok ? "Aceptado" : "Rechazado",
                    Encf = documento.Encf,
                    CodigoError = ok ? null : (codigo.Length > 0 ? codigo : "ACECF"),
                    Mensajes = resp.Mensajes.Count > 0
                        ? resp.Mensajes
                        : (string.IsNullOrWhiteSpace(resp.Mensaje) ? new List<string>() : new List<string> { resp.Mensaje }),
                    XmlSinFirmar = xml,
                    XmlFirmado = xmlFirmado,
                    XmlRespuesta = resp.Body
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ACECF {Encf}", documento.Encf);
                return FiscalEnvioResultado.Error("GATEWAY_ERROR", ex.Message);
            }
        }
    }
}
