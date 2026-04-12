using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreasController : ControllerBase
    {
        private readonly IAreas _IAreas;

        public AreasController(IAreas iAreas)
        {
            _IAreas = iAreas;
        }

        // GET: api/Areas/1
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<Area>> Get(int IdEmpresa)
        {
            return await _IAreas.GetAllAreas(IdEmpresa);
        }

        // GET: api/Areas/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<Area> GetById(int id)
        {
            return await _IAreas.GetAreaById(id);
        }

        // POST: api/Areas
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Area area)
        {
            if (area == null)
                return BadRequest("El área no puede ser nula");

            await _IAreas.InsertArea(area);
            return Ok(area);
        }

        // PUT: api/Areas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Area area)
        {
            if (area == null || id != area.IdArea)
                return BadRequest("Datos inválidos");

            _IAreas.UpdateArea(id, area);
            return NoContent();
        }

        // DELETE: api/Areas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _IAreas.DeleteArea(id);
            return NoContent();
        }
    }
}
