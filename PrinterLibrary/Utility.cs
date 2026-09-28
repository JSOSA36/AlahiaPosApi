using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PrinterLibrary
{
    public static class Utility
    {
        private static readonly string SecretKey = "clave-secreta-alahia"; // 👈 misma clave que uses en Angular
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("AlahiaSalt123"); // 👈 semilla fija

        public static string ReturnNameOfMonth(int id)
        {
            string Dia = string.Empty;
            switch (id)
            {
                case 1: Dia = "Ene"; break;
                case 2: Dia = "Feb"; break;
                case 3: Dia = "Mar"; break;
                case 4: Dia = "Abr"; break;
                case 5: Dia = "May"; break;
                case 6: Dia = "Jun"; break;
                case 7: Dia = "Jul"; break;
                case 8: Dia = "Ago"; break;
                case 9: Dia = "Sept"; break;
                case 10: Dia = "Oct"; break;
                case 11: Dia = "Nov"; break;
                case 12: Dia = "Dic"; break;
            }
            return Dia;
        }
        public static void Send(
         string smtpServer,
         int smtpPort,
         bool enableSsl,
         string smtpUser,
         string smtpPassword,
         string fromName,
         string to,
         string subject,
         string body
     )
        {
            Send(smtpServer, smtpPort, enableSsl, smtpUser, smtpPassword, fromName, to, subject, body, null, null);
        }

        public static void Send(
         string smtpServer,
         int smtpPort,
         bool enableSsl,
         string smtpUser,
         string smtpPassword,
         string fromName,
         string to,
         string subject,
         string body,
         byte[]? attachmentBytes,
         string? attachmentName
     )
        {
            var mail = new MailMessage
            {
                From = new MailAddress(smtpUser, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mail.To.Add(to);
            Attachment? attachment = null;
            try
            {
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    var stream = new MemoryStream(attachmentBytes);
                    attachment = new Attachment(stream, string.IsNullOrWhiteSpace(attachmentName) ? "recibo.pdf" : attachmentName, "application/pdf");
                    mail.Attachments.Add(attachment);
                }

                var smtp = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPassword),
                    EnableSsl = enableSsl
                };

                smtp.Send(mail);
            }
            finally
            {
                attachment?.Dispose();
                mail.Dispose();
            }
        }

        public static string ObtenerImagenCategoriaPorDefecto(string referencia)
        {
            if (string.IsNullOrWhiteSpace(referencia))
                return "https://alahiaupdate.alahiapos.com/1Productos de Belleza.jpg";

            referencia = referencia.Trim();

            return referencia switch
            {
                // 🧑‍🦱 Peluquería
                "Peluquería" =>
                    "https://alahiaupdate.alahiapos.com/51177eaf-7404-4039-99c4-6356283d49ea.jpg",

                // 💅 Uñas
                "Uñas" =>
                    "https://alahiaupdate.alahiapos.com/1Uñas.jpg",

                // ✨ Estética
                "Estética" =>
                    "https://alahiaupdate.alahiapos.com/b7ef0eb8-8d43-4b18-8462-c7c452335084.jpg",

                // 🪒 Depilación
                "Depilación" =>
                    "https://alahiaupdate.alahiapos.com/1b1b492d-175c-45c8-b805-7b3fa1a231bf.jpg",

                // 👁️ Cejas y Pestañas
                "Cejas y Pestañas" =>
                    "https://alahiaupdate.alahiapos.com/49b2b3b0-f0c6-4c92-828c-73df9f77910d.jpg",

                // 💄 Maquillaje
                "Maquillaje" =>
                    "https://alahiaupdate.alahiapos.com/1Aplicacion de Servicios.jpg",

                // 🧖 Spa
                "Spa" =>
                    "https://alahiaupdate.alahiapos.com/6d265933-8f3a-4d46-bf9b-53222fe3aeec.jpg",

                // 🧴 Fallback genérico
                _ =>
                    "https://alahiaupdate.alahiapos.com/1Productos de Belleza.jpg"
            };
        }

        public static string UploadFileFtp(byte[] imageBytes, string imageName)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            var safeName = SanitizarNombreArchivoFtp(imageName);
            string publicUrl = $"https://alahiaupdate.alahiapos.com/{safeName}";
            string ftpUrl = $"ftp://alahiaupdate.alahiapos.com/{safeName}";

            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.UsePassive = true;
                request.UseBinary = true;
                request.KeepAlive = false;
                request.EnableSsl = false;
                request.Credentials = new NetworkCredential("administrator", "JoelAriel8787");
                request.ContentLength = imageBytes.Length;

                using (Stream ftpStream = request.GetRequestStream())
                {
                    ftpStream.Write(imageBytes, 0, imageBytes.Length);
                }

                // Cerrar bien la operación FTP; sin GetResponse la conexión queda a medias
                // y el siguiente upload puede fallar hasta reiniciar el proceso.
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    _ = response.StatusDescription;
                }

                return publicUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al subir archivo '{safeName}': {ex.Message}");
                return null;
            }
        }

        private static string SanitizarNombreArchivoFtp(string imageName)
        {
            var raw = Path.GetFileName(string.IsNullOrWhiteSpace(imageName)
                ? Guid.NewGuid().ToString("N") + ".jpg"
                : imageName.Trim());

            var sb = new StringBuilder(raw.Length);
            foreach (var ch in raw)
            {
                if (char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_')
                    sb.Append(ch);
                else
                    sb.Append('_');
            }

            var safe = sb.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(safe) || safe == "." || safe == "..")
                safe = Guid.NewGuid().ToString("N") + ".jpg";
            if (!safe.Contains('.'))
                safe += ".jpg";
            return safe;
        }

        // 🔐 Encriptar un texto (ej. id de empresa)
        public static string EncryptString(string plainText)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(SecretKey, Salt, 1000);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                    sw.Close();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // 🔓 Desencriptar un texto encriptado
        public static string DecryptString(string cipherText)
        {
            var buffer = Convert.FromBase64String(cipherText);
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(SecretKey, Salt, 1000);
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var ms = new MemoryStream(buffer))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        // 👌 Helper: genera la URL de cita para la empresa
        public static string GenerarUrlCita(Guid guid)
        {
            return $"https://alahiapos.com/citas/{guid}";
        }
        public static string GenerarUrlCatalogo(Guid guid)
        {
            return $"https://alahiabeautysalonapp.alahiapos.com/catalogo/{guid}";
        }

        // 🔑 Generar contraseña aleatoria segura
        public static string GenerarPasswordAleatoria(int length = 6)
        {
            const string chars = "0123456789";
            var random = new RNGCryptoServiceProvider();
            var result = new char[length];
            var buffer = new byte[sizeof(uint)];

            for (int i = 0; i < length; i++)
            {
                random.GetBytes(buffer);
                uint num = BitConverter.ToUInt32(buffer, 0);
                result[i] = chars[(int)(num % (uint)chars.Length)];
            }

            return new string(result);
        }


        // 🔒 Hashear contraseña con SHA256 (puedes cambiar a BCrypt si quieres más seguridad)
        public static string EncriptarPassword(string plainPassword)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plainPassword));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
