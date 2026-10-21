using System.Net.Mail;
using AlahiaPos.Entities.Domain;
using PrinterLibrary;

namespace AlahiaPosApi.Servicios
{
    public static class FacturaCorreoEnvio
    {
        public static void Enviar(Empresas empresa, string destino, string asunto, string html)
        {
            if (empresa == null)
                throw new InvalidOperationException("Empresa no encontrada.");

            if (string.IsNullOrWhiteSpace(empresa.ServidorSMTP)
                || string.IsNullOrWhiteSpace(empresa.CorreoSMTP)
                || string.IsNullOrWhiteSpace(empresa.PasswordSMTP)
                || empresa.PuertoSMTP is not > 0)
            {
                throw new InvalidOperationException(
                    "La empresa no tiene correo SMTP configurado. Complete servidor, puerto, usuario y clave en la empresa.");
            }

            var fromName = string.IsNullOrWhiteSpace(empresa.NombreRemitente)
                ? (empresa.NombreComercial ?? "Factura")
                : empresa.NombreRemitente.Trim();

            Utility.Send(
                empresa.ServidorSMTP.Trim(),
                empresa.PuertoSMTP.Value,
                empresa.UsaSSL ?? true,
                empresa.CorreoSMTP.Trim(),
                empresa.PasswordSMTP,
                fromName,
                destino.Trim(),
                asunto,
                html);
        }
    }
}
