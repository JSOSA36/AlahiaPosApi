using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RncCLienteDGIIController : ControllerBase
    {
        private readonly IRNCService _rncService;

        public RncCLienteDGIIController(
            IRNCService rncService
        )
        {
            _rncService = rncService;
        }

        // =====================================================
        // 🔥 CONSULTAR RNC / CÉDULA
        // =====================================================

        // GET:
        // api/RncCLienteDGII/40212312312

        [HttpGet("{rnc}")]
        public async Task<IActionResult> Consultar(
            string rnc)
        {
            if (string.IsNullOrWhiteSpace(rnc))
            {
                return BadRequest(new
                {
                    message =
                        "Debe indicar un RNC o cédula"
                });
            }

            var cliente = await _rncService
                .ConsultarAsync(rnc);

            if (cliente == null)
            {
                return NotFound(new
                {
                    message =
                        "Cliente no encontrado"
                });
            }

            return Ok(cliente);
        }
    }
}