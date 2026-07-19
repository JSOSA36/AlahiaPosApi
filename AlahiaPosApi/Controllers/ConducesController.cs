using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConducesController : ControllerBase
    {
        private readonly IConducesService _service;

        public ConducesController(IConducesService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa:int}")]
        public async Task<IActionResult> Listar(
            int idEmpresa,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? idFactura = null,
            [FromQuery] string? q = null)
        {
            try
            {
                var data = await _service.ListarAsync(idEmpresa, desde, hasta, idFactura, q);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Detalle/{idConduce:int}/{idEmpresa:int}")]
        public async Task<IActionResult> GetById(int idConduce, int idEmpresa)
        {
            try
            {
                var data = await _service.GetByIdAsync(idConduce, idEmpresa);
                if (data == null) return NotFound(new { message = "Conduce no encontrado." });
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("FacturasDisponibles/{idEmpresa:int}")]
        public async Task<IActionResult> FacturasDisponibles(
            int idEmpresa,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] string? q = null)
        {
            try
            {
                var data = await _service.FacturasDisponiblesAsync(idEmpresa, desde, hasta, q);
                return Ok(data);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("LineasPendientes/{idFactura:int}/{idEmpresa:int}")]
        public async Task<IActionResult> LineasPendientes(int idFactura, int idEmpresa)
        {
            try
            {
                var data = await _service.LineasPendientesAsync(idFactura, idEmpresa);
                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("EstadoEntrega/{idFactura:int}/{idEmpresa:int}")]
        public async Task<IActionResult> EstadoEntrega(int idFactura, int idEmpresa)
        {
            try
            {
                var data = await _service.EstadoEntregaAsync(idFactura, idEmpresa);
                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearConduceRequest request)
        {
            try
            {
                var data = await _service.CrearAsync(request);
                return Ok(data);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{idConduce:int}/{idEmpresa:int}")]
        public async Task<IActionResult> Anular(int idConduce, int idEmpresa)
        {
            try
            {
                await _service.AnularAsync(idConduce, idEmpresa);
                return Ok(new { message = "Conduce anulado." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
