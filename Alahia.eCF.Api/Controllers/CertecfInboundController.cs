using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Seguridad;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Alahia.eCF.Api.Controllers
{
    /// <summary>
    /// Receptor que DGII llama en CerteCF (pasos 8–11).
    /// Patrón oficial: https://{host}/{ambiente}/{servicio}/fe/{recepcion|aprobacioncomercial|autenticacion}/api/...
    /// </summary>
    [ApiController]
    [Route("{ambiente}/{rnc}/fe")]
    public class CertecfInboundController : ControllerBase
    {
        private readonly AlahiaPosContext? _ctx;
        private readonly DgiiCertificadoResolver _certs;
        private readonly ILogger<CertecfInboundController> _logger;

        public CertecfInboundController(
            DgiiCertificadoResolver certs,
            ILogger<CertecfInboundController> logger,
            IServiceProvider services)
        {
            _certs = certs;
            _logger = logger;
            _ctx = services.GetService<AlahiaPosContext>();
        }

        [HttpGet("autenticacion/api/semilla")]
        public IActionResult Semilla()
        {
            var xml =
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?><SemillaModel><valor>{Guid.NewGuid():N}</valor><fecha>{DateTime.UtcNow:o}</fecha></SemillaModel>";
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        [HttpPost("autenticacion/api/validacioncertificado")]
        public IActionResult ValidarCertificado()
        {
            return Ok(new { token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()), expira = DateTime.UtcNow.AddHours(1) });
        }

        [HttpPost("recepcion/api/ecf")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> RecepcionEcf(string ambiente, string rnc, CancellationToken ct)
        {
            string? xmlIn = null;
            string? encf = null;
            Empresas? empresa = null;
            try
            {
                xmlIn = await ReadXmlAsync(ct);
                if (string.IsNullOrWhiteSpace(xmlIn))
                    return BadRequest(new { codigo = 2, error = "XML ausente" });

                TryParseEcf(xmlIn, out var rncEmisor, out var rncComprador, out encf);
                empresa = await FindEmpresaAsync(rnc, ct);
                var arecf = ArecfXmlBuilder.Build(
                    rncEmisor ?? "",
                    rncComprador ?? CertecfReceptorUrls.Digits(rnc),
                    encf ?? "E000000000000",
                    estado: 0);
                arecf = await FirmarArecfAsync(arecf, empresa, ct);

                await LogAsync(empresa?.IdEmpresa, rnc, "ECF", encf, "Recibido", ambiente, ct);
                return Content(arecf, "text/xml", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Recepción e-CF falló RNC {Rnc} eNCF {Encf}", rnc, encf);
                await LogAsync(empresa?.IdEmpresa, rnc, "ECF", encf, "Error", Cortar(ex.Message, 900), ct);
                return StatusCode(500, new { codigo = 2, error = "No se pudo acusar el e-CF" });
            }
        }

        [HttpPost("aprobacioncomercial/api/ecf")]
        public async Task<IActionResult> RecepcionAcecf(string ambiente, string rnc, CancellationToken ct)
        {
            var xmlIn = await ReadXmlAsync(ct);
            TryParseAcecf(xmlIn, out var encf);
            var empresa = await FindEmpresaAsync(rnc, ct);
            await LogAsync(empresa?.IdEmpresa, rnc, "ACECF", encf, "OK", ambiente, ct);
            return Ok(new { codigo = 1, estado = "OK" });
        }

        private async Task<string> ReadXmlAsync(CancellationToken ct)
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(ct);
                var file = form.Files.GetFile("xml") ?? form.Files.FirstOrDefault();
                if (file != null)
                {
                    using var sr = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
                    return await sr.ReadToEndAsync(ct);
                }
                if (form.TryGetValue("xml", out var raw))
                    return raw.ToString();
            }

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            return await reader.ReadToEndAsync(ct);
        }

        private async Task<Empresas?> FindEmpresaAsync(string rnc, CancellationToken ct)
        {
            if (_ctx == null) return null;
            var d = CertecfReceptorUrls.Digits(rnc);
            if (string.IsNullOrWhiteSpace(d)) return null;

            var empresas = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.RNC != null && e.RNC != "")
                .Select(e => new { e.IdEmpresa, e.RNC })
                .ToListAsync(ct);
            var idPorRnc = empresas.FirstOrDefault(e => CertecfReceptorUrls.Digits(e.RNC) == d)?.IdEmpresa;
            if (idPorRnc is int idEmpresa)
                return await _ctx.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpresa == idEmpresa, ct);

            // CerteCF declara el RNC del e-CF (Dra Sena 133659115), no siempre Empresas.RNC.
            var idSesion = await _ctx.CertecfCasos.AsNoTracking()
                .Where(c => c.PayloadJson.Contains(d))
                .Select(c => (int?)c.IdSesion)
                .FirstOrDefaultAsync(ct);
            if (idSesion is int sesionId)
            {
                var idDueno = await _ctx.CertecfSesiones.AsNoTracking()
                    .Where(s => s.IdSesion == sesionId)
                    .Select(s => (int?)s.IdEmpresa)
                    .FirstOrDefaultAsync(ct);
                if (idDueno is int dueno)
                    return await _ctx.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpresa == dueno, ct);
            }

            // El portal CerteCF entra con 133659115. Empresas.RNC de Dra Sena no es ese número.
            if (d == CertecfArtefactos.RncDraSena)
                return await _ctx.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.IdEmpresa == 60, ct);
            return null;
        }

        /// <summary>
        /// IIS no tiene perfil de usuario: la clave del .p12 se abre en memoria.
        /// Si la firma falla, DGII ve InternalServerError y reinicia la prueba.
        /// </summary>
        private async Task<string> FirmarArecfAsync(string arecf, Empresas? empresa, CancellationToken ct)
        {
            if (_ctx == null || empresa == null) return arecf;
            var cert = await _ctx.CertificadosDigitales.AsNoTracking()
                .Where(c => c.IdEmpresa == empresa.IdEmpresa && c.Activo && c.ArchivoBytes != null)
                .OrderByDescending(c => c.FechaCreacion)
                .FirstOrDefaultAsync(ct);
            if (cert?.ArchivoBytes is not { Length: > 0 })
            {
                var material = await _certs.ResolveAsync(empresa.IdEmpresa, ct);
                return _certs.Firmar(arecf, material);
            }

            try
            {
                using var enMemoria = new X509Certificate2(
                    cert.ArchivoBytes,
                    cert.PasswordEncriptado,
                    X509KeyStorageFlags.EphemeralKeySet);
                return XmlSigner.SignXml(arecf, enMemoria);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Firma ARECF en memoria falló. Reintento con almacén de máquina.");
                using var enMaquina = new X509Certificate2(
                    cert.ArchivoBytes,
                    cert.PasswordEncriptado,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                return XmlSigner.SignXml(arecf, enMaquina);
            }
        }

        private static string? Cortar(string? s, int max)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            s = s.Trim();
            return s.Length <= max ? s : s[..max];
        }

        private async Task LogAsync(int? idEmpresa, string rnc, string tipo, string? encf, string estado, string ambiente, CancellationToken ct)
        {
            if (_ctx == null) return;
            _ctx.CertecfInboundLogs.Add(new CertecfInboundLog
            {
                IdEmpresa = idEmpresa,
                Rnc = CertecfReceptorUrls.Digits(rnc),
                Tipo = tipo,
                Encf = encf,
                Estado = estado,
                Mensaje = ambiente,
                Fecha = DateTime.Now
            });
            await _ctx.SaveChangesAsync(ct);
        }

        private static void TryParseEcf(string xml, out string? rncEmisor, out string? rncComprador, out string? encf)
        {
            rncEmisor = rncComprador = encf = null;
            try
            {
                var doc = XDocument.Parse(xml);
                encf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName is "eNCF" or "ENCF")?.Value;
                rncEmisor = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCEmisor")?.Value;
                rncComprador = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCComprador")?.Value;
            }
            catch { /* best-effort */ }
        }

        private static void TryParseAcecf(string xml, out string? encf)
        {
            encf = null;
            try
            {
                var doc = XDocument.Parse(xml);
                encf = doc.Descendants().FirstOrDefault(x => x.Name.LocalName is "eNCF" or "ENCF")?.Value;
            }
            catch { /* best-effort */ }
        }
    }
}
