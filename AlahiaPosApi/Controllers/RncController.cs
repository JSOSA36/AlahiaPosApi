
using Alahia_Pos.Services;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Alahia_Pos.Controllers
{
    [ApiController]
    [Route("api/rnc")]
    public class RncController : ControllerBase
    {
        private readonly IRNCService _rncService;

        public RncController(IRNCService rncService)
        {
            _rncService = rncService;
        }

        // 🔍 GET: api/rnc/133307847
        [HttpGet("{rnc}")]
        public async Task<ActionResult<ClienteDgiiDto>> Consultar(string rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc))
                return BadRequest("Debe enviar un RNC o Cédula");

            var result = await _rncService.ConsultarAsync(rnc);

            if (result == null)
                return NotFound("Cliente no encontrado");

            return Ok(result);
        }
    }
}