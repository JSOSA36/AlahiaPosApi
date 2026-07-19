using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialServiciosController : ControllerBase
    {
        private readonly IHistorialServicios _historialServicios;

        public HistorialServiciosController(IHistorialServicios historialServicios)
        {
            _historialServicios = historialServicios;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] int idEmpresa,
            [FromQuery] int idCliente,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest("idEmpresa es requerido.");
            }

            if (idCliente <= 0)
            {
                return BadRequest("idCliente es requerido.");
            }

            var resultado = await _historialServicios.GetHistorialAsync(
                idEmpresa,
                idCliente,
                desde,
                hasta);

            return Ok(resultado);
        }

        [HttpGet("ultimo")]
        public async Task<IActionResult> GetUltimoServicio(
            [FromQuery] int idEmpresa,
            [FromQuery] int idCliente)
        {
            if (idEmpresa <= 0 || idCliente <= 0)
            {
                return BadRequest("idEmpresa e idCliente son requeridos.");
            }

            var resultado = await _historialServicios.GetUltimoServicioAsync(
                idEmpresa,
                idCliente);

            if (resultado == null)
            {
                return NotFound();
            }

            return Ok(resultado);
        }
    }
}
