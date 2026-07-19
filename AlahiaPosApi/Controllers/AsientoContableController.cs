using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AsientoContableController : ControllerBase
    {
        private readonly IAsientoContableService _service;

        public AsientoContableController(IAsientoContableService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<AsientoContable>> Get(
            int idEmpresa,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            return await _service.GetByEmpresaAsync(idEmpresa, desde, hasta);
        }

        [HttpGet("consultar/{idEmpresa}")]
        public async Task<IEnumerable<AsientoContable>> Consultar(
            int idEmpresa,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? idCuentaContable = null,
            [FromQuery] string? numero = null,
            [FromQuery] string? concepto = null)
        {
            return await _service.ConsultarAsync(
                idEmpresa, desde, hasta, idCuentaContable, numero, concepto);
        }

        [HttpGet("GetById/{id}/{idEmpresa}")]
        public async Task<ActionResult<AsientoContable>> GetById(int id, int idEmpresa)
        {
            var asiento = await _service.GetByIdAsync(id, idEmpresa);
            if (asiento == null)
                return NotFound();

            return asiento;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AsientoContable entity)
        {
            try
            {
                var id = await _service.CreateAsync(entity);
                return Ok(new { id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] AsientoContable entity)
        {
            if (id != entity.IdAsientoContable)
                return BadRequest();

            try
            {
                await _service.UpdateAsync(entity);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("anular/{id}/{idEmpresa}")]
        public async Task<IActionResult> Anular(int id, int idEmpresa)
        {
            try
            {
                await _service.AnularAsync(id, idEmpresa);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
