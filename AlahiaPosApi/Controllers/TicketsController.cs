using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketsService _service;

        public TicketsController(ITicketsService service)
        {
            _service = service;
        }

        [HttpPost("crear")]
        public async Task<IActionResult> Crear([FromForm] CrearTicketDto dto)
        {
            try
            {
                var result = await _service.CrearAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Crear ticket sin sesión ERP: valida UserName + Password.
        /// Pensado para login y pantallas de bloqueo (acceso, pago, políticas).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("crear-desde-login")]
        public async Task<IActionResult> CrearDesdeLogin([FromForm] CrearTicketDesdeLoginDto dto)
        {
            try
            {
                var result = await _service.CrearDesdeLoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("empresa/{idEmpresa:int}")]
        public async Task<IActionResult> ListarEmpresa(int idEmpresa)
        {
            try
            {
                return Ok(await _service.ListarPorEmpresaAsync(idEmpresa));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("admin/listar")]
        [RequiereEmpresaSistema]
        [PermitirEmpresaObjetivo]
        public async Task<IActionResult> ListarAdmin([FromQuery] TicketFiltroAdminDto filtro)
        {
            try
            {
                return Ok(await _service.ListarAdminAsync(filtro));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{idTicket:int}")]
        public async Task<IActionResult> Obtener(int idTicket, [FromQuery] int? idEmpresa)
        {
            try
            {
                return Ok(await _service.ObtenerAsync(idTicket, idEmpresa));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{idTicket:int}/mensaje")]
        public async Task<IActionResult> Mensaje(int idTicket, [FromForm] AgregarTicketMensajeDto dto)
        {
            try
            {
                dto.IdTicket = idTicket;
                var result = await _service.AgregarMensajeAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("admin/{idTicket:int}/estado")]
        [RequiereEmpresaSistema]
        [PermitirEmpresaObjetivo]
        public async Task<IActionResult> CambiarEstado(int idTicket, [FromBody] CambiarTicketEstadoDto dto)
        {
            try
            {
                dto.IdTicket = idTicket;
                var result = await _service.CambiarEstadoAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("metricas")]
        [RequiereEmpresaSistema]
        [PermitirEmpresaObjetivo]
        public async Task<IActionResult> Metricas([FromQuery] int? idEmpresa)
        {
            try
            {
                return Ok(await _service.ObtenerMetricasAsync(idEmpresa));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("notificaciones/{idEmpresa:int}")]
        public async Task<IActionResult> Notificaciones(int idEmpresa, [FromQuery] int? idUsuario, [FromQuery] bool soloNoLeidas = false)
        {
            try
            {
                return Ok(await _service.ListarNotificacionesAsync(idEmpresa, idUsuario, soloNoLeidas));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("notificaciones/{idNotificacion:int}/leer")]
        public async Task<IActionResult> MarcarLeida(int idNotificacion, [FromQuery] int idEmpresa)
        {
            try
            {
                await _service.MarcarNotificacionLeidaAsync(idNotificacion, idEmpresa);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("notificaciones/leer-todas")]
        public async Task<IActionResult> MarcarTodas([FromQuery] int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                await _service.MarcarTodasLeidasAsync(idEmpresa, idUsuario);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
