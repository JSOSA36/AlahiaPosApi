using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/citas-publicas")]
    [ApiController]
    [AllowAnonymous]
    public class CitasPublicasController : ControllerBase
    {
        private readonly ICitasPublicasService _svc;

        public CitasPublicasController(ICitasPublicasService svc)
        {
            _svc = svc;
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> Salon(string guid)
        {
            try
            {
                return Ok(await _svc.ObtenerSalonAsync(guid));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{guid}/disponibilidad")]
        public async Task<IActionResult> Disponibilidad(string guid, [FromQuery] int idEmpleado, [FromQuery] string fecha)
        {
            try
            {
                return Ok(await _svc.ObtenerDisponibilidadAsync(guid, idEmpleado, fecha));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{guid}/mias")]
        public async Task<IActionResult> Mias(string guid, [FromQuery] string? telefono)
        {
            try
            {
                return Ok(await _svc.ListarCitasClienteAsync(guid, telefono ?? ""));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{guid}")]
        public async Task<IActionResult> Crear(string guid, [FromForm] CitaPublicaCrearRequest request)
        {
            try
            {
                return Ok(await _svc.CrearCitaAsync(guid, request ?? new CitaPublicaCrearRequest()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;
                return BadRequest(new { message = inner.Message });
            }
        }
    }
}
