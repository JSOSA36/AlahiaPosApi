using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/DgiiFiscal")]
    [ApiController]
    public class DgiiFiscalReconciliacionController : ControllerBase
    {
        private readonly IFiscalReconciliacionService _reconciliacion;
        private readonly IFiscalWorkEnqueueService _enqueue;
        private readonly IDgiiFiscalAuthService _auth;

        public DgiiFiscalReconciliacionController(
            IFiscalReconciliacionService reconciliacion,
            IFiscalWorkEnqueueService enqueue,
            IDgiiFiscalAuthService auth)
        {
            _reconciliacion = reconciliacion;
            _enqueue = enqueue;
            _auth = auth;
        }

        [HttpGet("{idEmpresa:int}/pendientes")]
        public async Task<ActionResult<IReadOnlyList<FiscalReconciliacionItemDto>>> Pendientes(
            int idEmpresa,
            [FromQuery] int top = 100)
            => Ok(await _reconciliacion.ListarPendientesAsync(idEmpresa, top));

        /// <summary>Reproceso explícito: encola trabajo ForceReprocess (worker lo procesa).</summary>
        [HttpPost("{idEmpresa:int}/reprocesar")]
        public async Task<IActionResult> Reprocesar(
            int idEmpresa,
            [FromBody] FiscalDocumentoRequest request,
            [FromHeader(Name = "X-IdUsuario")] int idUsuarioHeader = 0)
        {
            if (request == null)
                return BadRequest();

            try
            {
                var token = Request.Headers.Authorization.FirstOrDefault();
                var idUsuario = idUsuarioHeader > 0 ? idUsuarioHeader : request.IdUsuario;
                if (idUsuario <= 0)
                    return Unauthorized(new { message = "X-IdUsuario o request.IdUsuario requerido." });

                await _auth.EnsureCanReprocessAsync(token, idEmpresa, idUsuario);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }

            request.IdEmpresa = idEmpresa;
            request.ForceReprocess = true;
            await _enqueue.EnqueueFotografiaSiActivoAsync(request);
            return Ok(new { message = "Reproceso fiscal encolado." });
        }
    }
}
