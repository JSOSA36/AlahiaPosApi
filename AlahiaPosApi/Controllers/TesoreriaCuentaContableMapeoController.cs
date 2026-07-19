using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesoreriaCuentaContableMapeoController : ControllerBase
    {
        private readonly ITesoreriaCuentaContableMapeoService _service;

        public TesoreriaCuentaContableMapeoController(ITesoreriaCuentaContableMapeoService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<TesoreriaCuentaContableMapeo>> GetByEmpresa(int idEmpresa)
        {
            return await _service.GetByEmpresaAsync(idEmpresa);
        }

        [HttpGet("ByCuenta/{idEmpresa}/{idCuentaFinanciera}")]
        public async Task<ActionResult<TesoreriaCuentaContableMapeo?>> GetByCuenta(
            int idEmpresa,
            int idCuentaFinanciera)
        {
            var mapeo = await _service.GetByCuentaFinancieraAsync(idEmpresa, idCuentaFinanciera);
            if (mapeo == null)
                return NotFound();

            return Ok(mapeo);
        }

        [HttpPut]
        public async Task<IActionResult> Upsert([FromBody] TesoreriaCuentaContableMapeo mapeo)
        {
            if (mapeo == null)
                return BadRequest();

            var id = await _service.UpsertAsync(mapeo);
            return Ok(new { id });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
