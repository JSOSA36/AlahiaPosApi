using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [PermitirEmpresaObjetivo]
    public class EmpresaCargosRecurrentesController : ControllerBase
    {
        private readonly IEmpresaCargoRecurrenteService _service;

        public EmpresaCargosRecurrentesController(IEmpresaCargoRecurrenteService service)
        {
            _service = service;
        }

        [HttpGet("empresa/{idEmpresa}")]
        public async Task<IActionResult> Listar(int idEmpresa, [FromQuery] bool soloActivos = false)
        {
            var list = await _service.ListarPorEmpresaAsync(idEmpresa, soloActivos);
            return Ok(list);
        }

        [HttpGet("calculo/{idEmpresa}")]
        public async Task<IActionResult> Calculo(int idEmpresa)
        {
            try
            {
                var calc = await _service.CalcularFacturaAsync(idEmpresa);
                return Ok(calc);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequiereEmpresaSistema]
        public async Task<IActionResult> Crear([FromBody] CrearEmpresaCargoRecurrenteDto dto)
        {
            try
            {
                var created = await _service.CrearAsync(dto);
                return Ok(created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut]
        [RequiereEmpresaSistema]
        public async Task<IActionResult> Actualizar([FromBody] ActualizarEmpresaCargoRecurrenteDto dto)
        {
            try
            {
                var updated = await _service.ActualizarAsync(dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/desactivar")]
        [RequiereEmpresaSistema]
        public async Task<IActionResult> Desactivar(int id, [FromQuery] int? idUsuario = null)
        {
            try
            {
                await _service.DesactivarAsync(id, idUsuario);
                return Ok(new { message = "Cargo desactivado" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
