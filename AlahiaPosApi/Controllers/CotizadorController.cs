using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto.Cotizador;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class CotizadorController : ControllerBase
    {
        private readonly ICotizadorService _cotizador;

        public CotizadorController(ICotizadorService cotizador)
        {
            _cotizador = cotizador;
        }

        /// <summary>Catálogo dinámico: tipos de negocio, módulos comerciales, dependencias y parámetros.</summary>
        [HttpGet("catalogo")]
        public async Task<ActionResult<CotizadorCatalogoDto>> Catalogo()
        {
            try
            {
                return Ok(await _cotizador.ObtenerCatalogoAsync());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>Calcula propuesta en tiempo real. No requiere registro.</summary>
        [HttpPost("calcular")]
        public async Task<ActionResult<CotizadorPropuestaDto>> Calcular([FromBody] CotizadorCalcularRequest request)
        {
            try
            {
                return Ok(await _cotizador.CalcularAsync(request ?? new CotizadorCalcularRequest()));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("guardar")]
        public async Task<ActionResult<CotizadorGuardarResponse>> Guardar([FromBody] CotizadorGuardarRequest request)
        {
            try
            {
                return Ok(await _cotizador.GuardarAsync(request ?? new CotizadorGuardarRequest()));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("enviar-correo")]
        public async Task<IActionResult> EnviarCorreo([FromBody] CotizadorEnviarCorreoRequest request)
        {
            try
            {
                await _cotizador.EnviarCorreoAsync(request ?? new CotizadorEnviarCorreoRequest());
                return Ok(new { message = "Cotización enviada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("solicitar")]
        public async Task<IActionResult> Solicitar([FromBody] CotizadorSolicitarRequest request)
        {
            try
            {
                await _cotizador.SolicitarAsync(request ?? new CotizadorSolicitarRequest());
                return Ok(new { message = "Solicitud registrada. Un especialista le contactará." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Vista pública (sin login): el cliente abre el link compartido por WhatsApp/correo.
        /// </summary>
        [HttpGet("publica/{folio}")]
        public async Task<ActionResult<CotizadorVistaPublicaDto>> VistaPublica(string folio)
        {
            try
            {
                var vista = await _cotizador.ObtenerVistaPublicaAsync(folio);
                if (vista == null)
                    return NotFound(new { message = "Cotización no encontrada o enlace inválido." });
                return Ok(vista);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
