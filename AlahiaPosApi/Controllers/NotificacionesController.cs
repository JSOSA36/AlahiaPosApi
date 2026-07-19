using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificacionesController : ControllerBase
    {
        private readonly INotificacionCentro _centro;

        public NotificacionesController(INotificacionCentro centro)
        {
            _centro = centro;
        }

        [HttpGet("contador/{idEmpresa:int}")]
        public async Task<IActionResult> Contador(int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                var n = await _centro.ContarNoLeidasAsync(idEmpresa, idUsuario);
                return Ok(new { noLeidas = n });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("listar/{idEmpresa:int}")]
        public async Task<IActionResult> Listar(
            int idEmpresa,
            [FromQuery] int? idUsuario,
            [FromQuery] bool soloNoLeidas = false,
            [FromQuery] int top = 50,
            [FromQuery] bool incluirArchivadas = false)
        {
            try
            {
                return Ok(await _centro.ListarAsync(idEmpresa, idUsuario, soloNoLeidas, top, incluirArchivadas));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{idNotificacion:int}/leer")]
        public async Task<IActionResult> MarcarLeida(int idNotificacion, [FromQuery] int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                await _centro.MarcarLeidaAsync(idNotificacion, idEmpresa, idUsuario);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{idNotificacion:int}/no-leida")]
        public async Task<IActionResult> MarcarNoLeida(int idNotificacion, [FromQuery] int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                await _centro.MarcarNoLeidaAsync(idNotificacion, idEmpresa, idUsuario);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{idNotificacion:int}/archivar")]
        public async Task<IActionResult> Archivar(int idNotificacion, [FromQuery] int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                await _centro.ArchivarAsync(idNotificacion, idEmpresa, idUsuario);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("leer-todas")]
        public async Task<IActionResult> MarcarTodas([FromQuery] int idEmpresa, [FromQuery] int? idUsuario)
        {
            try
            {
                await _centro.MarcarTodasAsync(idEmpresa, idUsuario);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
