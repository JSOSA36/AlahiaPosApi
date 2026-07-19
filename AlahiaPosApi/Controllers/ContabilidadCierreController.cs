using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadCierreController : ControllerBase
    {
        private readonly IContabilidadCierreService _service;

        public ContabilidadCierreController(IContabilidadCierreService service)
        {
            _service = service;
        }

        [HttpGet("periodo/{idEmpresa}/{anio}/{mes}")]
        public async Task<ActionResult<PeriodoContableDto>> GetPeriodo(int idEmpresa, int anio, int mes)
        {
            try
            {
                var periodo = await _service.GetPeriodoAsync(idEmpresa, anio, mes);
                return periodo == null ? NotFound() : periodo;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("periodos/{idEmpresa}/{anio}")]
        public async Task<IEnumerable<PeriodoContableDto>> GetPeriodos(int idEmpresa, int anio)
        {
            return await _service.GetPeriodosAsync(idEmpresa, anio);
        }

        [HttpPost("cerrar")]
        public async Task<ActionResult<PeriodoContableDto>> CerrarPeriodo([FromBody] CerrarPeriodoRequest request)
        {
            try
            {
                var resultado = await _service.CerrarPeriodoAsync(request);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
