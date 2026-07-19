using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivosFijosController : ControllerBase
    {
        private readonly IActivosFijosService _service;

        public ActivosFijosController(IActivosFijosService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IActionResult> Listar(
            int idEmpresa,
            [FromQuery] string? estado = null,
            [FromQuery] string? texto = null)
        {
            var result = await _service.ListarAsync(idEmpresa, estado, texto);
            return Ok(result);
        }

        [HttpGet("Resumen/{idEmpresa}")]
        public async Task<IActionResult> Resumen(int idEmpresa)
        {
            var result = await _service.ObtenerResumenAsync(idEmpresa);
            return Ok(result);
        }

        [HttpGet("Detalle/{id}/{idEmpresa}")]
        public async Task<IActionResult> Obtener(int id, int idEmpresa)
        {
            var result = await _service.ObtenerPorIdAsync(id, idEmpresa);
            if (result == null)
                return NotFound(new { message = "Activo fijo no encontrado." });
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarActivoFijoRequest request)
        {
            try
            {
                var result = await _service.ActualizarAsync(id, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }
    }
}
