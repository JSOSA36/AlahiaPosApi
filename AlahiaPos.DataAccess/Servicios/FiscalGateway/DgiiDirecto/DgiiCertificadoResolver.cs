using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto
{
    public class DgiiCertificadoMaterial
    {
        public X509Certificate2 Certificate { get; init; } = null!;
        public string Source { get; init; } = "";
    }

    public class DgiiCertificadoResolver
    {
        private readonly AlahiaPosContext? _ctx;
        private readonly DgiiDirectoSettings _settings;
        private readonly ILogger<DgiiCertificadoResolver> _logger;

        public DgiiCertificadoResolver(
            IOptions<DgiiDirectoSettings> settings,
            ILogger<DgiiCertificadoResolver> logger,
            AlahiaPosContext? ctx = null)
        {
            _ctx = ctx;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<DgiiCertificadoMaterial> ResolveAsync(int idEmpresa, CancellationToken ct = default)
        {
            if (idEmpresa <= 0)
                idEmpresa = DgiiEmpresaContext.Current;

            if (_ctx != null && idEmpresa > 0)
            {
                var cert = await _ctx.CertificadosDigitales.AsNoTracking()
                    .Where(c => c.IdEmpresa == idEmpresa && c.Activo)
                    .OrderByDescending(c => c.FechaCreacion)
                    .FirstOrDefaultAsync(ct);

                if (cert != null)
                {
                    var password = cert.PasswordEncriptado;
                    if (cert.ArchivoBytes is { Length: > 0 })
                    {
                        return new DgiiCertificadoMaterial
                        {
                            Certificate = new X509Certificate2(
                                cert.ArchivoBytes,
                                password,
                                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable),
                            Source = $"DB:{cert.IdCertificado}"
                        };
                    }

                    if (!string.IsNullOrWhiteSpace(cert.RutaArchivo) && File.Exists(cert.RutaArchivo))
                    {
                        return new DgiiCertificadoMaterial
                        {
                            Certificate = new X509Certificate2(
                                cert.RutaArchivo,
                                password,
                                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable),
                            Source = $"File:{cert.RutaArchivo}"
                        };
                    }

                    _logger.LogWarning(
                        "CertificadoDigital {Id} de empresa {Empresa} sin bytes/ruta usable.",
                        cert.IdCertificado, idEmpresa);
                    throw new InvalidOperationException(
                        "El certificado digital de la empresa no tiene el archivo .p12. Cárguelo otra vez en Facturación electrónica.");
                }
            }

            var fallback = TryFromSettings();
            if (fallback != null) return fallback;

            throw new InvalidOperationException(
                "No hay certificado digital configurado. Cárguelo en Facturación electrónica (.p12 / .pfx).");
        }

        public string Firmar(string xml, DgiiCertificadoMaterial material)
            => XmlSigner.SignXml(xml, material.Certificate);

        private DgiiCertificadoMaterial? TryFromSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.P12Path) || string.IsNullOrWhiteSpace(_settings.P12Password))
                return null;
            if (!File.Exists(_settings.P12Path))
            {
                _logger.LogWarning(
                    "P12 de appsettings no existe ({Path}). Se usa CertificadoDigital de la empresa.",
                    _settings.P12Path);
                return null;
            }

            return new DgiiCertificadoMaterial
            {
                Certificate = new X509Certificate2(
                    _settings.P12Path,
                    _settings.P12Password,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable),
                Source = $"Config:{_settings.P12Path}"
            };
        }
    }
}
