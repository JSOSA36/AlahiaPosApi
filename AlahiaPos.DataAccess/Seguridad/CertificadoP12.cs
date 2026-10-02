extern alias bcrypto;

using bcrypto::Org.BouncyCastle.Asn1.Pkcs;
using bcrypto::Org.BouncyCastle.Pkcs;
using bcrypto::Org.BouncyCastle.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AlahiaPos.DataAccess.Seguridad
{
    /// <summary>
    /// Los .p12 nuevos de la DGII vienen cifrados con AES (OpenSSL 3).
    /// Windows, en el servidor, responde "Bad Data" aunque la contraseña sea la correcta.
    /// Si el almacén no lo abre, se lee con BouncyCastle y se guarda en 3DES, que sí abre.
    /// </summary>
    public static class CertificadoP12
    {
        public sealed class Abierto : IDisposable
        {
            public X509Certificate2 Certificado { get; }
            public byte[] BytesCompatibles { get; }

            public Abierto(X509Certificate2 certificado, byte[] bytesCompatibles)
            {
                Certificado = certificado;
                BytesCompatibles = bytesCompatibles;
            }

            public void Dispose() => Certificado.Dispose();
        }

        public static Abierto Abrir(byte[] bytes, string password)
        {
            if (bytes is not { Length: > 0 })
                throw new InvalidOperationException("El archivo del certificado está vacío.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Indique la contraseña del certificado.");

            bytes = NormalizarContenedor(bytes);

            var nativo = IntentarNativo(bytes, password);
            if (nativo != null)
            {
                if (!nativo.HasPrivateKey)
                {
                    nativo.Dispose();
                    throw new InvalidOperationException("El certificado no contiene llave privada. Use el .p12/.pfx de firma.");
                }
                return new Abierto(nativo, bytes);
            }

            byte[] legado;
            try
            {
                legado = Reempaquetar(bytes, password);
            }
            catch (IOException ex) when (EsClaveIncorrecta(ex))
            {
                throw new InvalidOperationException("La contraseña del certificado no es correcta.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "No se pudo abrir el certificado. Revise el archivo .p12 y la contraseña. " + ex.Message);
            }

            var abierto = IntentarNativo(legado, password)
                ?? throw new InvalidOperationException(
                    "No se pudo abrir el certificado. Revise el archivo .p12 y la contraseña.");
            if (!abierto.HasPrivateKey)
            {
                abierto.Dispose();
                throw new InvalidOperationException("El certificado no contiene llave privada. Use el .p12/.pfx de firma.");
            }
            return new Abierto(abierto, legado);
        }

        private static X509Certificate2? IntentarNativo(byte[] bytes, string password)
        {
            X509KeyStorageFlags[] modos =
            {
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable,
                X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable,
                X509KeyStorageFlags.EphemeralKeySet,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable
            };

            foreach (var modo in modos)
            {
                try
                {
                    return new X509Certificate2(bytes, password, modo);
                }
                catch (CryptographicException)
                {
                    /* el siguiente modo, o BouncyCastle */
                }
            }
            return null;
        }

        private static byte[] Reempaquetar(byte[] bytes, string password)
        {
            using var input = new MemoryStream(bytes);
            var store = new Pkcs12StoreBuilder().Build();
            store.Load(input, password.ToCharArray());

            var legacy = new Pkcs12StoreBuilder()
                .SetKeyAlgorithm(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc)
                .SetCertAlgorithm(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc)
                .Build();

            var conLlave = false;
            foreach (string alias in store.Aliases)
            {
                if (store.IsKeyEntry(alias))
                {
                    conLlave = true;
                    legacy.SetKeyEntry(alias, store.GetKey(alias), store.GetCertificateChain(alias));
                }
                else if (store.IsCertificateEntry(alias))
                {
                    legacy.SetCertificateEntry(alias, store.GetCertificate(alias));
                }
            }

            if (!conLlave)
                throw new InvalidOperationException("El certificado no contiene llave privada. Use el .p12/.pfx de firma.");

            using var output = new MemoryStream();
            legacy.Save(output, password.ToCharArray(), new SecureRandom());
            return output.ToArray();
        }

        private static bool EsClaveIncorrecta(Exception ex)
        {
            var msg = ex.Message ?? "";
            return msg.Contains("MAC", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("password", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("contraseña", StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] NormalizarContenedor(byte[] bytes)
        {
            if (bytes[0] == 0x30)
                return bytes;

            var texto = LeerTexto(bytes);
            if (string.IsNullOrWhiteSpace(texto))
                return bytes;

            var inicio = texto.IndexOf("-----BEGIN", StringComparison.Ordinal);
            if (inicio >= 0)
            {
                var finCabecera = texto.IndexOf('\n', inicio);
                var fin = texto.IndexOf("-----END", StringComparison.Ordinal);
                if (finCabecera > 0 && fin > finCabecera)
                {
                    var cuerpo = texto.Substring(finCabecera, fin - finCabecera);
                    var decodificado = DecodificarBase64(cuerpo);
                    if (decodificado is { Length: > 0 } && decodificado[0] == 0x30)
                        return decodificado;
                }
            }

            var plano = DecodificarBase64(texto);
            if (plano is { Length: > 0 } && plano[0] == 0x30)
                return plano;
            return bytes;
        }

        private static string? LeerTexto(byte[] bytes)
        {
            try
            {
                return System.Text.Encoding.UTF8.GetString(bytes).Trim();
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? DecodificarBase64(string texto)
        {
            var limpio = new string(texto.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (limpio.Length < 16 || limpio.Length % 4 != 0)
                return null;
            try
            {
                return Convert.FromBase64String(limpio);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
