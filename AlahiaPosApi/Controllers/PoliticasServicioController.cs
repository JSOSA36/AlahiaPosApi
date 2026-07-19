using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoliticasServicioController : ControllerBase
    {
        private readonly IPoliticasServicioService _service;

        public PoliticasServicioController(IPoliticasServicioService service)
        {
            _service = service;
        }

        [HttpGet("estado/{idEmpresa}")]
        public async Task<IActionResult> Estado(int idEmpresa, [FromQuery] int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest(new { message = "idUsuario es obligatorio." });

            var result = await _service.ObtenerEstadoAsync(idEmpresa, idUsuario);
            return Ok(result);
        }

        [HttpPost("aceptar")]
        public async Task<IActionResult> Aceptar([FromBody] AceptarPoliticasRequest request)
        {
            // Si el cliente no envía IP, tomar la del request
            if (string.IsNullOrWhiteSpace(request?.DireccionIp))
            {
                request ??= new AceptarPoliticasRequest();
                request.DireccionIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            }

            var result = await _service.AceptarAsync(request);
            if (!result.Exitoso)
                return BadRequest(result);

            return Ok(result);
        }

        // ========== Admin MacroBits ==========

        [HttpGet("versiones")]
        public async Task<IActionResult> ListarVersiones()
        {
            var list = await _service.ListarVersionesAsync();
            return Ok(list);
        }

        [HttpGet("versiones/{idVersion}")]
        public async Task<IActionResult> ObtenerVersion(int idVersion)
        {
            var v = await _service.ObtenerVersionAsync(idVersion);
            if (v == null)
                return NotFound(new { message = "Versión no encontrada." });
            return Ok(v);
        }

        [HttpPost("versiones")]
        public async Task<IActionResult> CrearBorrador([FromBody] CrearPoliticasVersionRequest request)
        {
            try
            {
                var result = await _service.CrearBorradorAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("versiones/publicar")]
        public async Task<IActionResult> Publicar([FromBody] PublicarPoliticasRequest request)
        {
            try
            {
                var result = await _service.PublicarAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("aceptaciones")]
        public async Task<IActionResult> ListarAceptaciones(
            [FromQuery] int? idVersion = null,
            [FromQuery] int? idEmpresa = null)
        {
            var list = await _service.ListarAceptacionesAsync(idVersion, idEmpresa);
            return Ok(list);
        }
    }
}
