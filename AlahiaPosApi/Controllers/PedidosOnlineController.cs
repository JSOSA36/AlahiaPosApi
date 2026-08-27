using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/pedidos-online")]
    [ApiController]
    [AllowAnonymous]
    public class PedidosOnlineController : ControllerBase
    {
        private readonly IPedidosOnlineService _svc;

        public PedidosOnlineController(IPedidosOnlineService svc)
        {
            _svc = svc;
        }

        [HttpGet("{slug}/seguimiento")]
        public async Task<IActionResult> Seguimiento(string slug, [FromQuery] int idPedidoOnline, [FromQuery] string? telefono)
        {
            try
            {
                return Ok(await _svc.ObtenerSeguimientoAsync(slug, idPedidoOnline, telefono ?? ""));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var inner = ex;
                while (inner.InnerException != null)
                    inner = inner.InnerException;
                return BadRequest(new { message = inner.Message });
            }
        }

        [HttpGet("{slug}/perfil")]
        public async Task<IActionResult> Perfil(string slug, [FromQuery] string? telefono)
        {
            try
            {
                return Ok(await _svc.ObtenerPerfilClienteAsync(slug, telefono ?? ""));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{slug}/perfil")]
        public async Task<IActionResult> GuardarPerfil(string slug, [FromBody] PedidoOnlinePerfilRequest request)
        {
            try
            {
                return Ok(await _svc.GuardarPerfilClienteAsync(slug, request ?? new PedidoOnlinePerfilRequest()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{slug}/mis-pedidos")]
        public async Task<IActionResult> MisPedidos(string slug, [FromQuery] string? telefono)
        {
            try
            {
                return Ok(await _svc.ListarPedidosClienteAsync(slug, telefono ?? ""));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("{slug}/menu")]
        public async Task<IActionResult> Menu(string slug)
        {
            try
            {
                return Ok(await _svc.ObtenerMenuAsync(slug));
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("{slug}")]
        public async Task<IActionResult> Crear(string slug, [FromBody] PedidoOnlineCheckoutRequest request)
        {
            try
            {
                return Ok(await _svc.CrearPedidoAsync(slug, request ?? new PedidoOnlineCheckoutRequest()));
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
