using System.Net;
using System.Net.Mail;
using System.Text;
using AlahiaBackup.Configuration;
using AlahiaBackup.Interfaces;
using AlahiaBackup.Models;
using Microsoft.Extensions.Options;

namespace AlahiaBackup.Services
{
    public sealed class BackupEmailService : IBackupEmailService
    {
        private readonly ILogger<BackupEmailService> _logger;
        private readonly BackupSettings _backup;
        private readonly SmtpSettings _smtp;

        public BackupEmailService(
            ILogger<BackupEmailService> logger,
            IOptions<BackupSettings> backup,
            IOptions<SmtpSettings> smtp)
        {
            _logger = logger;
            _backup = backup.Value;
            _smtp = smtp.Value;
        }

        public async Task<bool> EnviarAsync(
            IReadOnlyList<BackupArtifact> archivos,
            DateOnly fecha,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_backup.CorreoDestino))
                throw new InvalidOperationException("Falta Backup:CorreoDestino.");

            var maxBytes = Math.Max(1, _backup.MaxAdjuntoMb) * 1024L * 1024L;
            var maxPartes = Math.Max(1, _backup.MaxPartesCorreo);
            var enviadoAlguno = false;

            foreach (var archivo in archivos)
            {
                var partes = (int)Math.Ceiling(archivo.Bytes / (double)maxBytes);
                if (partes > maxPartes)
                {
                    var aviso = new StringBuilder();
                    aviso.AppendLine($"El respaldo de {archivo.BaseDeDatos} del {fecha:dd/MM/yyyy} quedó en el servidor.");
                    aviso.AppendLine($"Archivo: {archivo.Archivo}");
                    aviso.AppendLine($"Tamaño: {Mb(archivo.Bytes)} MB");
                    aviso.AppendLine($"SHA256: {archivo.Sha256}");
                    aviso.AppendLine();
                    aviso.AppendLine($"No se adjuntó porque ocupa {partes} correos y el límite configurado es {maxPartes}.");
                    aviso.AppendLine("Gmail no acepta un adjunto de ese tamaño en un solo mensaje.");
                    await EnviarMensajeAsync(
                        $"Backup {archivo.BaseDeDatos} {fecha:dd/MM/yyyy} quedó en el servidor",
                        aviso.ToString(),
                        null,
                        null,
                        cancellationToken);
                    continue;
                }

                await using var stream = new FileStream(archivo.Archivo, FileMode.Open, FileAccess.Read, FileShare.Read);
                var buffer = new byte[maxBytes];
                for (var parte = 1; parte <= partes; parte++)
                {
                    var leidos = await ReadChunkAsync(stream, buffer, cancellationToken);
                    var nombre = partes == 1
                        ? Path.GetFileName(archivo.Archivo)
                        : $"{Path.GetFileName(archivo.Archivo)}.{parte:000}";

                    var cuerpo = new StringBuilder();
                    cuerpo.AppendLine($"Respaldo de {archivo.BaseDeDatos} del {fecha:dd/MM/yyyy}.");
                    cuerpo.AppendLine(partes == 1
                        ? "El archivo va adjunto."
                        : $"Parte {parte} de {partes}. Hay que unirlas en orden para recuperar el archivo.");
                    cuerpo.AppendLine($"Tamaño total: {Mb(archivo.Bytes)} MB");
                    cuerpo.AppendLine($"SHA256 del archivo completo: {archivo.Sha256}");
                    if (partes > 1)
                    {
                        cuerpo.AppendLine();
                        cuerpo.AppendLine("Para unir en Windows, en la carpeta donde estén las partes:");
                        cuerpo.Append("copy /b ");
                        cuerpo.Append(string.Join("+", Enumerable.Range(1, partes).Select(n => $"{Path.GetFileName(archivo.Archivo)}.{n:000}")));
                        cuerpo.AppendLine($" {Path.GetFileName(archivo.Archivo)}");
                    }

                    var datos = new byte[leidos];
                    Buffer.BlockCopy(buffer, 0, datos, 0, leidos);
                    await EnviarMensajeAsync(
                        partes == 1
                            ? $"Backup {archivo.BaseDeDatos} {fecha:dd/MM/yyyy}"
                            : $"Backup {archivo.BaseDeDatos} {fecha:dd/MM/yyyy} parte {parte} de {partes}",
                        cuerpo.ToString(),
                        datos,
                        nombre,
                        cancellationToken);
                    enviadoAlguno = true;
                }
            }

            return enviadoAlguno;
        }

        public Task EnviarErrorAsync(string error, int intento, int maxIntentos, CancellationToken cancellationToken)
        {
            var cuerpo = $"El respaldo de Alahia no se completó (intento {intento} de {maxIntentos}).{Environment.NewLine}{Environment.NewLine}{error}";
            return EnviarMensajeAsync("Backup Alahia falló", cuerpo, null, null, cancellationToken);
        }

        private async Task EnviarMensajeAsync(
            string subject,
            string body,
            byte[]? adjunto,
            string? nombreAdjunto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_smtp.Host) || string.IsNullOrWhiteSpace(_smtp.User))
                throw new InvalidOperationException("Falta la configuración Smtp.");

            using var mail = new MailMessage
            {
                From = new MailAddress(_smtp.User, _smtp.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mail.To.Add(_backup.CorreoDestino);

            if (adjunto is { Length: > 0 })
            {
                var stream = new MemoryStream(adjunto);
                mail.Attachments.Add(new Attachment(stream, nombreAdjunto ?? "backup.bin", "application/octet-stream"));
            }

            using var smtp = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                Credentials = new NetworkCredential(_smtp.User, _smtp.Password),
                EnableSsl = _smtp.EnableSsl,
                Timeout = 300_000
            };

            _logger.LogInformation("Enviando correo \"{Asunto}\" a {Destino}", subject, _backup.CorreoDestino);
            cancellationToken.ThrowIfCancellationRequested();
            await smtp.SendMailAsync(mail, cancellationToken);
        }

        private static async Task<int> ReadChunkAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            var leidos = 0;
            while (leidos < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(leidos, buffer.Length - leidos), cancellationToken);
                if (n == 0)
                    break;
                leidos += n;
            }

            return leidos;
        }

        private static string Mb(long bytes) => (bytes / 1024d / 1024d).ToString("N1");
    }
}
