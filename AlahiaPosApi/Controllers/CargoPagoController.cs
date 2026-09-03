using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CargoPagoController : ControllerBase
    {
        private readonly ICargoPagoService _service;

        public CargoPagoController(ICargoPagoService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa:int}")]
        public async Task<ActionResult<IEnumerable<CargoPagoRegla>>> Get(int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("Empresa inválida.");
            return Ok(await _service.ListarAsync(idEmpresa));
        }

        [HttpPost]
        public async Task<ActionResult<CargoPagoRegla>> Post([FromBody] CargoPagoRegla regla)
        {
            try
            {
                var saved = await _service.GuardarAsync(regla);
                return Ok(saved);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CargoPagoRegla>> Put(int id, [FromBody] CargoPagoRegla regla)
        {
            if (regla == null || (id > 0 && regla.IdCargoPagoRegla != 0 && id != regla.IdCargoPagoRegla))
                return BadRequest();
            regla.IdCargoPagoRegla = id;
            try
            {
                return Ok(await _service.GuardarAsync(regla));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{idEmpresa:int}/{id:int}")]
        public async Task<IActionResult> Delete(int idEmpresa, int id)
        {
            await _service.EliminarAsync(idEmpresa, id);
            return NoContent();
        }

        [HttpPost("calcular")]
        public async Task<ActionResult<CargoPagoCalcularResult>> Calcular(
            [FromBody] CargoPagoCalcularRequest request)
        {
            if (request == null || request.IdEmpresa <= 0)
                return BadRequest("Empresa inválida.");

            var result = await _service.CalcularAsync(
                request.IdEmpresa,
                request.Metodos,
                request.BaseCalculo);
            return Ok(result);
        }
    }
}
