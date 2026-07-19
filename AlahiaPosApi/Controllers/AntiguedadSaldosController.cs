using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AntiguedadSaldosController : ControllerBase
    {
        private readonly IAntiguedadSaldosService _service;

        public AntiguedadSaldosController(IAntiguedadSaldosService service)
        {
            _service = service;
        }

        /// <summary>
        /// Antigüedad de saldos — Cuentas por Cobrar.
        /// </summary>
        [HttpGet("CxC/{idEmpresa:int}")]
        public async Task<IActionResult> CxC(
            int idEmpresa,
            [FromQuery] int idTercero = 0,
            [FromQuery] string? documento = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] bool soloVencidas = false,
            [FromQuery] bool soloPendientes = true,
            [FromQuery] DateTime? fechaCorte = null)
        {
            try
            {
                var result = await _service.ObtenerAntiguedadCxCAsync(new AntiguedadSaldosFiltroRequest
                {
                    IdEmpresa = idEmpresa,
                    IdTercero = idTercero,
                    Documento = documento,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    SoloVencidas = soloVencidas,
                    SoloPendientes = soloPendientes,
                    FechaCorte = fechaCorte
                });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Antigüedad de saldos — Cuentas por Pagar.
        /// </summary>
        [HttpGet("CxP/{idEmpresa:int}")]
        public async Task<IActionResult> CxP(
            int idEmpresa,
            [FromQuery] int idTercero = 0,
            [FromQuery] string? documento = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] bool soloVencidas = false,
            [FromQuery] bool soloPendientes = true,
            [FromQuery] DateTime? fechaCorte = null)
        {
            try
            {
                var result = await _service.ObtenerAntiguedadCxPAsync(new AntiguedadSaldosFiltroRequest
                {
                    IdEmpresa = idEmpresa,
                    IdTercero = idTercero,
                    Documento = documento,
                    FechaDesde = fechaDesde,
                    FechaHasta = fechaHasta,
                    SoloVencidas = soloVencidas,
                    SoloPendientes = soloPendientes,
                    FechaCorte = fechaCorte
                });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
