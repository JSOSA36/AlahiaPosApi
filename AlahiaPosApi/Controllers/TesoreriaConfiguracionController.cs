using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesoreriaConfiguracionController : ControllerBase
    {
        private readonly ITesoreriaConfiguracionService _service;

        public TesoreriaConfiguracionController(ITesoreriaConfiguracionService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<ActionResult<TesoreriaConfiguracion>> Get(int idEmpresa)
        {
            var config = await _service.GetByEmpresaAsync(idEmpresa);
            if (config == null)
                return NotFound();

            return Ok(config);
        }

        [HttpPut]
        public async Task<ActionResult<TesoreriaConfiguracion>> Put(
            [FromBody] TesoreriaConfiguracion configuracion)
        {
            if (configuracion == null)
                return BadRequest();

            var result = await _service.UpsertAsync(configuracion);
            return Ok(result);
        }
    }
}
