using AlahiaPos.Entities.Domain;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaNegocioController : ControllerBase
    {
        private readonly IAreaNegocio _IAreaNegocio;

        public AreaNegocioController(IAreaNegocio iAreaNegocio)
        {
            _IAreaNegocio = iAreaNegocio;
        }

        // GET: api/AreaNegocio/1
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<AreaNegocio>> Get(int IdEmpresa)
        {
            return await _IAreaNegocio.GetAll(IdEmpresa);
        }

        // GET: api/AreaNegocio/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<AreaNegocio> GetById(int id)
        {
            return await _IAreaNegocio.GetById(id);
        }

        // POST: api/AreaNegocio
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AreaNegocio areaNegocio)
        {
            if (areaNegocio == null)
                return BadRequest("El área de negocio no puede ser nula");

            await _IAreaNegocio.Add(areaNegocio);
            return Ok(areaNegocio);
        }

        // PUT: api/AreaNegocio/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] AreaNegocio areaNegocio)
        {
            if (areaNegocio == null || id != areaNegocio.IdAreaNegocio)
                return BadRequest("Datos inválidos");

            await _IAreaNegocio.Update(areaNegocio);
            return NoContent();
        }

        // DELETE: api/AreaNegocio/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _IAreaNegocio.Delete(id);
            return NoContent();
        }
    }
}