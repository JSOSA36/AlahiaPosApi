using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SucursalesController : ControllerBase
    {
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public SucursalesController(ISucursalService sucursales, ISesionTokenResolver tokens)
        {
            _sucursales = sucursales;
            _tokens = tokens;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SucursalSesionDto>>> Listar()
        {
            var sesion = await ResolverSesionAsync();
            if (sesion == null)
                return Unauthorized();

            var lista = await _sucursales.ListarPorUsuarioAsync(sesion.IdUsuario, sesion.IdEmpresa);
            return Ok(lista);
        }

        private async Task<SesionActual?> ResolverSesionAsync()
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null) return sesion;
            var auth = Request.Headers["Authorization"].FirstOrDefault();
            return await _tokens.ResolverAsync(auth, HttpContext.RequestAborted);
        }
    }

    [Route("api/Sesion")]
    [ApiController]
    public class SesionController : ControllerBase
    {
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public SesionController(ISucursalService sucursales, ISesionTokenResolver tokens)
        {
            _sucursales = sucursales;
            _tokens = tokens;
        }

        [HttpPost("sucursal")]
        [PermitirCambioSucursal]
        public async Task<ActionResult<CambiarSucursalResultado>> CambiarSucursal(
            [FromBody] CambiarSucursalRequest request)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null)
            {
                var auth = Request.Headers["Authorization"].FirstOrDefault();
                sesion = await _tokens.ResolverAsync(auth, HttpContext.RequestAborted);
            }
            if (sesion == null)
                return Unauthorized();

            if (request == null || request.IdSucursal <= 0)
                return BadRequest("IdSucursal requerido.");

            try
            {
                var dispositivo = Request.Headers["X-Pos-Device-Id"].FirstOrDefault()
                    ?? Request.Headers["User-Agent"].FirstOrDefault();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

                var resultado = await _sucursales.CambiarActivaAsync(
                    sesion.IdUsuario,
                    sesion.IdEmpresa,
                    request.IdSucursal,
                    dispositivo,
                    ip);

                sesion.IdSucursal = resultado.IdSucursal;
                return Ok(resultado);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { message = SesionHttp.ForbiddenOtraSucursal });
            }
        }

        [HttpGet("yo")]
        public async Task<ActionResult<object>> Yo()
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null)
            {
                var auth = Request.Headers["Authorization"].FirstOrDefault();
                sesion = await _tokens.ResolverAsync(auth, HttpContext.RequestAborted);
            }
            if (sesion == null)
                return Unauthorized();

            return Ok(new
            {
                idUsuario = sesion.IdUsuario,
                idEmpresa = sesion.IdEmpresa,
                idSucursal = sesion.IdSucursal,
                userName = sesion.UserName
            });
        }
    }
}
