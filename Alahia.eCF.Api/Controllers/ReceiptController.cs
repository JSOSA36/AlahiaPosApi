using Alahia.eCF.Api.Services;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.PgEInvoicing;
using AlahiaPos.Entities.Dto.Fiscal;
using Microsoft.AspNetCore.Mvc;

namespace Alahia.eCF.Api.Controllers
{
    /// <summary>
    /// Contrato compatible con PG.eInvoicing para que el ERP pueda apuntar aquí
    /// sin cambiar su gateway (solo BaseUrl + ApiKey).
    /// </summary>
    [ApiController]
    [Route("api/Receipt")]
    public class ReceiptController : ControllerBase
    {
        private readonly ReceiptOrchestrator _orch;
        private readonly DgiiXmlBuilder _xmlBuilder;

        public ReceiptController(ReceiptOrchestrator orch, DgiiXmlBuilder xmlBuilder)
        {
            _orch = orch;
            _xmlBuilder = xmlBuilder;
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken ct)
        {
            var ok = await _orch.HealthAsync(ct);
            return Ok(new { ok, info = _orch.Info() });
        }

        /// <summary>Listado vacío / ping (PG usa GET /api/Receipt para health).</summary>
        [HttpGet]
        public IActionResult List() => Ok(new { items = Array.Empty<object>(), info = _orch.Info() });

        [HttpPost]
        public async Task<IActionResult> Enviar([FromBody] PgDgiiDocumentDto documento, CancellationToken ct)
        {
            if (documento?.Encabezado?.IdDoc == null || string.IsNullOrWhiteSpace(documento.Encabezado.IdDoc.ENCF))
                return BadRequest(new { error = "Encabezado.IdDoc.eNCF es requerido" });

            try
            {
                using var _ = PushAmbienteFromHeader();
                // Auto-ruta: E32 < 250k → RFCE; E32 ≥ 250k u otros tipos → e-CF.
                var resp = await _orch.EnviarAsync(documento, ct);
                return ToActionResult(resp);
            }
            catch (FiscalValidationException ex)
            {
                return UnprocessableEntity(new { codigo = ex.Codigo, error = ex.Message });
            }
        }

        /// <summary>
        /// Forzar RFCE (debug). La app debe usar POST /api/Receipt, que enruta sola.
        /// </summary>
        [HttpPost("rfce")]
        public async Task<IActionResult> EnviarRfce([FromBody] PgDgiiDocumentDto documento, CancellationToken ct)
        {
            if (documento?.Encabezado?.IdDoc == null || string.IsNullOrWhiteSpace(documento.Encabezado.IdDoc.ENCF))
                return BadRequest(new { error = "Encabezado.IdDoc.eNCF es requerido" });
            if (documento.Encabezado.IdDoc.TipoeCF != 32)
                return BadRequest(new { error = "RFCE solo aplica a TipoeCF=32" });

            try
            {
                using var _ = PushAmbienteFromHeader();
                var resp = await _orch.EnviarRfceAsync(documento, ct);
                return ToActionResult(resp);
            }
            catch (FiscalValidationException ex)
            {
                return UnprocessableEntity(new { codigo = ex.Codigo, error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Devuelve la trama XML (sin firmar) y el canal que usaría el envío.
        /// </summary>
        [HttpPost("preview-xml")]
        public IActionResult PreviewXml([FromBody] PgDgiiDocumentDto documento)
        {
            if (documento?.Encabezado?.IdDoc == null || string.IsNullOrWhiteSpace(documento.Encabezado.IdDoc.ENCF))
                return BadRequest(new { error = "Encabezado.IdDoc.eNCF es requerido" });

            var fiscal = PgReceiptMapper.ToFiscal(documento);
            var canal = ReceiptOrchestrator.ResolverCanal(fiscal.Encabezado.TipoEcf, fiscal.Encabezado.MontoTotal);
            var xml = _xmlBuilder.Build(fiscal, DateTime.Now);
            var nombre = DgiiXmlBuilder.NombreArchivo(
                fiscal.Encabezado.RncEmisor,
                fiscal.Encabezado.Encf);

            return Ok(new
            {
                canal,
                umbralRfce = ReceiptOrchestrator.UmbralRfceMontoTotal,
                montoTotal = fiscal.Encabezado.MontoTotal,
                tipoeCF = fiscal.Encabezado.TipoEcf,
                nombreArchivo = nombre,
                rncEmisorNormalizado = DgiiXmlBuilder.NormalizarRnc(fiscal.Encabezado.RncEmisor),
                xml
            });
        }

        [HttpGet("jobs/{jobId:guid}")]
        public async Task<IActionResult> ConsultarJob(Guid jobId, CancellationToken ct)
        {
            var resp = await _orch.ConsultarJobAsync(jobId, ct);
            if (resp == null) return NotFound(new { error = "Job no encontrado" });
            return Ok(resp);
        }

        [HttpPost("jobs/{jobId:guid}/retry")]
        public async Task<IActionResult> ReenviarJob(Guid jobId, CancellationToken ct)
        {
            try
            {
                var resp = await _orch.ReenviarJobAsync(jobId, User?.Identity?.Name ?? "api", ct);
                return ToActionResult(resp);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{trackId}")]
        public async Task<IActionResult> Consultar(string trackId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                return BadRequest(new { error = "trackId requerido" });

            var resp = await _orch.ConsultarAsync(trackId, ct);
            return Ok(resp);
        }

        private IDisposable PushAmbienteFromHeader()
        {
            Request.Headers.TryGetValue("X-Dgii-Ambiente", out var values);
            var ambiente = values.FirstOrDefault();
            return DgiiAmbienteContext.Push(ambiente);
        }

        private static IActionResult ToActionResult(PgTrackIdResponse resp)
        {
            var ok = string.Equals(resp.estado, "Aceptado", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(resp.estado, "AceptadoCondicional", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(resp.estado, "EnProceso", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(resp.estado, "PendienteEnvio", StringComparison.OrdinalIgnoreCase)
                     || (!string.IsNullOrWhiteSpace(resp.trackId) && resp.codigo == "1");

            // RFCE aceptado suele venir sin trackId (respuesta síncrona).
            if (!ok && string.IsNullOrWhiteSpace(resp.trackId) &&
                !string.Equals(resp.codigo, "1", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(resp.estado, "PendienteEnvio", StringComparison.OrdinalIgnoreCase))
                return new UnprocessableEntityObjectResult(resp);

            return new OkObjectResult(resp);
        }
    }
}
