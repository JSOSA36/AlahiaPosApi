using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using PrinterLibrary;

namespace AlahiaPos.DataAccess.Servicios.Suscripciones
{
    public class EmailSuscripcionCanal : INotificacionSuscripcionCanal
    {
        private readonly ILogger<EmailSuscripcionCanal> _logger;
        public string Canal => SuscripcionEstados.CanalEmail;

        public EmailSuscripcionCanal(ILogger<EmailSuscripcionCanal> logger)
        {
            _logger = logger;
        }

        public Task EnviarAsync(NotificacionSuscripcionMensaje mensaje, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mensaje.CorreoDestino))
            {
                _logger.LogWarning("Sin correo para empresa {IdEmpresa}", mensaje.IdEmpresa);
                return Task.CompletedTask;
            }

            try
            {
                var body = $@"
                    <div style=""font-family:Segoe UI,Arial,sans-serif;max-width:640px;"">
                    <h2>{System.Net.WebUtility.HtmlEncode(mensaje.Titulo)}</h2>
                    <p>Empresa: <strong>{System.Net.WebUtility.HtmlEncode(mensaje.NombreEmpresa ?? "")}</strong></p>
                    <p style=""line-height:1.5;"">{string.Join("<br/>", (mensaje.Mensaje ?? "").Replace("\r\n", "\n").Split('\n').Select(System.Net.WebUtility.HtmlEncode))}</p>
                    <p style=""color:#666;font-size:12px;"">MacroBits SRL / Alahia ERP</p>
                    </div>";

                Utility.Send(
                    "smtp.gmail.com",
                    587,
                    true,
                    "ing.joelarielsosa@gmail.com",
                    "wrcsdhewqdgrtula",
                    "MacroBits Software",
                    mensaje.CorreoDestino.Trim(),
                    mensaje.Titulo,
                    body
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email suscripción a {Email}", mensaje.CorreoDestino);
            }

            return Task.CompletedTask;
        }
    }

    public class InAppSuscripcionCanal : INotificacionSuscripcionCanal
    {
        private readonly ILogger<InAppSuscripcionCanal> _logger;
        public string Canal => SuscripcionEstados.CanalInApp;

        public InAppSuscripcionCanal(ILogger<InAppSuscripcionCanal> logger)
        {
            _logger = logger;
        }

        public Task EnviarAsync(NotificacionSuscripcionMensaje mensaje, CancellationToken ct = default)
        {
            // Persistido como SuscripcionEvento por el servicio orquestador.
            _logger.LogInformation(
                "InApp aviso {Tipo} empresa {Id}: {Mensaje}",
                mensaje.TipoAviso, mensaje.IdEmpresa, mensaje.Mensaje);
            return Task.CompletedTask;
        }
    }

    public class WhatsAppSuscripcionCanalStub : INotificacionSuscripcionCanal
    {
        private readonly ILogger<WhatsAppSuscripcionCanalStub> _logger;
        public string Canal => SuscripcionEstados.CanalWhatsApp;

        public WhatsAppSuscripcionCanalStub(ILogger<WhatsAppSuscripcionCanalStub> logger)
        {
            _logger = logger;
        }

        public Task EnviarAsync(NotificacionSuscripcionMensaje mensaje, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "[WhatsApp stub] {Tipo} empresa {Id}: {Mensaje}",
                mensaje.TipoAviso, mensaje.IdEmpresa, mensaje.Mensaje);
            return Task.CompletedTask;
        }
    }
}
