using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadConfiguracionController : ControllerBase
    {
        private readonly IContabilidadConfiguracionService _service;

        public ContabilidadConfiguracionController(IContabilidadConfiguracionService service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<ActionResult<ContabilidadConfiguracionDto>> Get(int idEmpresa)
        {
            try
            {
                return await _service.GetConfiguracionAsync(idEmpresa);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<ActionResult<ContabilidadConfiguracionDto>> Put(
            [FromBody] ActualizarContabilidadConfiguracionRequest request)
        {
            try
            {
                return await _service.ActualizarAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
