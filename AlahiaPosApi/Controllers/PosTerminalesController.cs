using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PosTerminalesController : ControllerBase
    {
        private readonly IPosTerminalService _service;

        public PosTerminalesController(IPosTerminalService service)
        {
            _service = service;
        }

        /// <summary>Registra o valida este PC contra el cupo de licencias POS de la empresa.</summary>
        [HttpPost("claim")]
        public async Task<IActionResult> Claim([FromBody] PosTerminalClaimRequest? req)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null)
                return Unauthorized(new { message = SesionHttp.UnauthorizedToken });

            var body = req ?? new PosTerminalClaimRequest();
            if (string.IsNullOrWhiteSpace(body.DeviceId))
            {
                body.DeviceId = Request.Headers[RequiereTerminalPosFilter.HeaderName].FirstOrDefault() ?? "";
            }

            var result = await _service.ClaimAsync(
                sesion.IdEmpresa,
                sesion.IdUsuario,
                body,
                sesion.EsEmpresaSistema);

            if (!result.Permitido)
            {
                return StatusCode(403, new
                {
                    message = result.Mensaje,
                    codigo = result.Codigo,
                    limite = result.Limite,
                    usadas = result.Usadas
                });
            }

            return Ok(result);
        }
    }
}
