using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriaGastoController : ControllerBase
    {
        private readonly ICategoriaGastoService _service;

        public CategoriaGastoController(ICategoriaGastoService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IActionResult> Get(int idEmpresa, [FromQuery] bool soloActivos = false)
        {
            var list = await _service.GetByEmpresaAsync(idEmpresa, soloActivos);
            return Ok(list);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
                return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CategoriaGasto entity)
        {
            try
            {
                if (entity == null)
                    return BadRequest();

                var created = await _service.CreateAsync(entity);
                return Ok(new { success = true, data = created });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] CategoriaGasto entity)
        {
            try
            {
                if (entity == null || id != entity.IdCategoriaGasto)
                    return BadRequest();

                await _service.UpdateAsync(entity);
                return Ok(new { success = true, message = "Categoría actualizada." });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.SoftDeleteAsync(id);
                return Ok(new { success = true, message = "Categoría desactivada." });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}
