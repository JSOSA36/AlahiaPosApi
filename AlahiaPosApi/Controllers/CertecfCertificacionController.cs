using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/CertecfCertificacion")]
    [RequiereEmpresaSistema]
    [PermitirEmpresaObjetivo]
    public class CertecfCertificacionController : ControllerBase
    {
        private readonly ICertecfCertificacionService _service;

        public CertecfCertificacionController(ICertecfCertificacionService service)
        {
            _service = service;
        }

        [HttpGet("estado/{idEmpresa:int}")]
        public async Task<IActionResult> Estado(int idEmpresa, CancellationToken ct)
        {
            try { return Ok(await _service.GetEstadoAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return StatusCode(500, new { message = MensajeAmigable(ex) }); }
        }

        [HttpPost("postulacion/{idEmpresa:int}")]
        public async Task<IActionResult> Postulacion(int idEmpresa, [FromBody] CertecfPostulacionDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.GuardarPostulacionAsync(idEmpresa, dto ?? new CertecfPostulacionDto(), ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("postulacion/{idEmpresa:int}/firmar")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> FirmarPostulacion(int idEmpresa, IFormFile archivo, CancellationToken ct)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Suba el XML que genera el portal CerteCF (GENERAR ARCHIVO)." });

            var ext = Path.GetExtension(archivo.FileName ?? "").ToLowerInvariant();
            if (ext is not ".xml" and not ".txt" and not "")
                return BadRequest(new { message = "Solo se acepta el XML de postulación del portal." });

            try
            {
                await using var stream = archivo.OpenReadStream();
                return FileResult(await _service.FirmarPostulacionXmlAsync(
                    idEmpresa, archivo.FileName ?? "postulacion.xml", stream, ct));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("paso/{idEmpresa:int}")]
        public async Task<IActionResult> MarcarPaso(int idEmpresa, [FromBody] CertecfMarcarPasoDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.MarcarPasoAsync(idEmpresa, dto, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("postulacion/{idEmpresa:int}/xml")]
        public async Task<IActionResult> PostulacionXml(int idEmpresa, CancellationToken ct)
        {
            try { return FileResult(await _service.GenerarPostulacionXmlAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("declaracion/{idEmpresa:int}/xml")]
        public async Task<IActionResult> DeclaracionXml(int idEmpresa, CancellationToken ct)
        {
            try { return FileResult(await _service.GenerarDeclaracionJuradaAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("ri/{idEmpresa:int}/lote")]
        public async Task<IActionResult> RiLote(int idEmpresa, CancellationToken ct)
        {
            try { return Ok(await _service.GenerarLoteRiAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("ri/{idEmpresa:int}/{idCaso:int}")]
        public async Task<IActionResult> Ri(int idEmpresa, int idCaso, CancellationToken ct)
        {
            try { return FileResult(await _service.GenerarRiAsync(idEmpresa, idCaso, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("excel/{idEmpresa:int}")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> CargarExcel(
            int idEmpresa,
            IFormFile archivo,
            [FromForm] int? idUsuario,
            CancellationToken ct)
        {
            if (archivo == null || archivo.Length == 0)
                return BadRequest(new { message = "Suba el Excel descargado de CerteCF (paso 2 e-CF o paso 3 ACECF)." });

            var ext = Path.GetExtension(archivo.FileName ?? "").ToLowerInvariant();
            if (ext is not ".xlsx" and not ".xls")
                return BadRequest(new { message = "Solo se acepta Excel (.xlsx)." });

            try
            {
                await using var stream = archivo.OpenReadStream();
                var sesion = await _service.CargarExcelAsync(
                    idEmpresa, idUsuario, archivo.FileName ?? "set.xlsx", stream, ct);
                return Ok(sesion);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("set/{idEmpresa:int}/reiniciar")]
        public async Task<IActionResult> ReiniciarSet(int idEmpresa, CancellationToken ct)
        {
            try { return Ok(await _service.ReiniciarSetDatosAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("simulacion/{idEmpresa:int}")]
        public async Task<IActionResult> Simulacion(int idEmpresa, CancellationToken ct)
        {
            try { return Ok(await _service.GenerarSimulacionAsync(idEmpresa, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("sesion/{idEmpresa:int}/{idSesion:int}")]
        public async Task<IActionResult> Sesion(int idEmpresa, int idSesion, CancellationToken ct)
        {
            try { return Ok(await _service.GetSesionAsync(idEmpresa, idSesion, ct)); }
            catch (InvalidOperationException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpPost("caso/{idEmpresa:int}/{idCaso:int}/enviar")]
        public async Task<IActionResult> EnviarCaso(int idEmpresa, int idCaso, CancellationToken ct)
        {
            try { return Ok(await _service.EnviarCasoAsync(idEmpresa, idCaso, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("caso/{idEmpresa:int}/{idCaso:int}/consultar")]
        public async Task<IActionResult> ConsultarCaso(int idEmpresa, int idCaso, CancellationToken ct)
        {
            try { return Ok(await _service.ConsultarCasoAsync(idEmpresa, idCaso, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        private static FileContentResult FileResult(CertecfArchivoDto a)
        {
            var bytes = Encoding.UTF8.GetBytes(a.Contenido ?? "");
            return new FileContentResult(bytes, a.ContentType ?? "application/xml")
            {
                FileDownloadName = a.NombreArchivo
            };
        }

        private static string MensajeAmigable(Exception ex)
        {
            var raw = ex.InnerException?.Message ?? ex.Message ?? "";
            if (raw.Contains("CertificadosDigitales", StringComparison.OrdinalIgnoreCase))
                return "Reinicie AlahiaPosApi: el certificado se lee de CertificadoDigital.";
            if (raw.Contains("Invalid object name", StringComparison.OrdinalIgnoreCase))
                return "Error de base de datos. Reinicie la API e intente de nuevo.";
            return raw.Length > 180 ? raw[..180] + "…" : raw;
        }
    }
}
