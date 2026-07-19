using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentaContableController : ControllerBase
    {
        private readonly ICuentaContableService _service;
        private readonly IContabilidadCatalogoService _catalogoService;

        public CuentaContableController(
            ICuentaContableService service,
            IContabilidadCatalogoService catalogoService)
        {
            _service = service;
            _catalogoService = catalogoService;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<CuentaContable>> Get(int idEmpresa)
        {
            return await _service.GetByEmpresaAsync(idEmpresa);
        }

        [HttpGet("arbol/{idEmpresa}")]
        public async Task<IEnumerable<CuentaContable>> GetArbol(int idEmpresa)
        {
            return await _service.GetArbolByEmpresaAsync(idEmpresa);
        }

        [HttpGet("GetById/{id}/{idEmpresa}")]
        public async Task<ActionResult<CuentaContable>> GetById(int id, int idEmpresa)
        {
            var cuenta = await _service.GetByIdAsync(id, idEmpresa);
            if (cuenta == null)
                return NotFound();

            return cuenta;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CuentaContable entity)
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
        public async Task<IActionResult> Put(int id, [FromBody] CuentaContable entity)
        {
            if (id != entity.IdCuentaContable)
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

        [HttpDelete("{id}/{idEmpresa}")]
        public async Task<IActionResult> Delete(int id, int idEmpresa)
        {
            try
            {
                await _service.DeleteAsync(id, idEmpresa);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("seed/{idEmpresa}")]
        public async Task<IActionResult> Seed(int idEmpresa)
        {
            await _catalogoService.SeedCatalogoDefaultAsync(idEmpresa);
            return Ok();
        }
    }
}
