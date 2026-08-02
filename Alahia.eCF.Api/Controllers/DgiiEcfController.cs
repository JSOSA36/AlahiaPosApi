using Alahia.eCF.Api.Services;
using AlahiaPos.DataAccess.Servicios.FiscalGateway.DgiiDirecto;
using Microsoft.AspNetCore.Mvc;

namespace Alahia.eCF.Api.Controllers
{
    [ApiController]
    [Route("api/ecf")]
    public class DgiiEcfController : ControllerBase
    {
        private readonly ReceiptOrchestrator _orch;

        public DgiiEcfController(ReceiptOrchestrator orch) => _orch = orch;

        [HttpGet("health")]
        public async Task<IActionResult> Health(CancellationToken ct)
        {
            using var _ = PushAmbienteFromHeader();
            var ok = await _orch.HealthAsync(ct);
            return Ok(new { ok, info = _orch.Info() });
        }

        [HttpGet("semilla")]
        public async Task<IActionResult> Semilla(CancellationToken ct)
        {
            using var _ = PushAmbienteFromHeader();
            var xml = await _orch.SemillaAsync(ct);
            return Content(xml, "application/xml");
        }

        private IDisposable PushAmbienteFromHeader()
        {
            Request.Headers.TryGetValue("X-Dgii-Ambiente", out var values);
            return DgiiAmbienteContext.Push(values.FirstOrDefault());
        }
    }
}
