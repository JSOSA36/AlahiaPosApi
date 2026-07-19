using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuscripcionCobrosController : ControllerBase
    {
        private readonly ISuscripcionCobroService _service;

        public SuscripcionCobrosController(ISuscripcionCobroService service)
        {
            _service = service;
        }

        [HttpPost("procesar-diario")]
        public async Task<IActionResult> ProcesarDiario()
        {
            await _service.ProcesarCicloDiarioAsync();
            return Ok(new { message = "Ciclo procesado" });
        }

        [HttpGet("resumen")]
        public async Task<IActionResult> Resumen()
        {
            return Ok(await _service.ObtenerResumenAsync());
        }

        /// <summary>Clientes (no sistema) para que MacroBits asigne cargos adicionales.</summary>
        [HttpGet("empresas")]
        public async Task<IActionResult> Empresas()
        {
            return Ok(await _service.ListarEmpresasCobroAsync());
        }

        [HttpGet("ciclos")]
        public async Task<IActionResult> Ciclos([FromQuery] int? idEmpresa = null)
        {
            return Ok(await _service.ListarCiclosAsync(idEmpresa));
        }

        [HttpGet("eventos/{idEmpresa}")]
        public async Task<IActionResult> Eventos(int idEmpresa, [FromQuery] int top = 100)
        {
            return Ok(await _service.ListarEventosAsync(idEmpresa, top));
        }

        [HttpGet("calculo/{idEmpresa}")]
        public async Task<IActionResult> Calculo(int idEmpresa)
        {
            try
            {
                return Ok(await _service.CalcularFacturaAsync(idEmpresa));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Precio especial del plan solo para un cliente (ej. Standard 80 → 70).
        /// Enviar precioPlanEspecialUsd null para volver al catálogo.
        /// </summary>
        [HttpPut("precio-plan-especial")]
        public async Task<IActionResult> PrecioPlanEspecial([FromBody] ActualizarPrecioPlanEspecialDto dto)
        {
            try
            {
                return Ok(await _service.ActualizarPrecioPlanEspecialAsync(dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("ciclo/{idCiclo}/detalle")]
        public async Task<IActionResult> DetalleCiclo(int idCiclo)
        {
            return Ok(await _service.ListarDetalleCicloAsync(idCiclo));
        }

        [HttpPost("actualizar-estado/{idEmpresa}")]
        public async Task<IActionResult> ActualizarEstado(int idEmpresa)
        {
            await _service.ActualizarEstadoEmpresaAsync(idEmpresa);
            return Ok(new { message = "Estado actualizado" });
        }
    }
}
