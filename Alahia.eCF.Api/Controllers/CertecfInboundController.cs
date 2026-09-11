using System.Text;
using System.Xml.Linq;
using AlahiaPos.DataAccess.Data;
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
        public async Task<IActionResult> RecepcionEcf(string ambiente, string rnc, CancellationToken ct)
        {
            var xmlIn = await ReadXmlAsync(ct);
            if (string.IsNullOrWhiteSpace(xmlIn))
                return BadRequest(new { codigo = 2, error = "XML ausente" });

            TryParseEcf(xmlIn, out var rncEmisor, out var rncComprador, out var encf);
            var empresa = await FindEmpresaAsync(rnc, ct);
            var arecf = ArecfXmlBuilder.Build(
                rncEmisor ?? "",
                rncComprador ?? CertecfReceptorUrls.Digits(rnc),
                encf ?? "E000000000000",
                estado: 0);

            if (empresa != null)
            {
                try
                {
                    var material = await _certs.ResolveAsync(empresa.IdEmpresa, ct);
                    arecf = _certs.Firmar(arecf, material);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ARECF firma falló RNC {Rnc}", rnc);
                    return StatusCode(500, new { codigo = 2, error = "No se pudo firmar ARECF" });
                }
            }

            await LogAsync(empresa?.IdEmpresa, rnc, "ECF", encf, "Recibido", ambiente, ct);
            return Content(arecf, "application/xml", Encoding.UTF8);
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
            var list = await _ctx.Empresas.AsNoTracking()
                .Where(e => e.RNC != null && e.RNC != "")
                .ToListAsync(ct);
            return list.FirstOrDefault(e => CertecfReceptorUrls.Digits(e.RNC) == d);
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
