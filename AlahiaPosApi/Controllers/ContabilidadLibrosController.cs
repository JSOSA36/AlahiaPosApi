using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadLibrosController : ControllerBase
    {
        private readonly IContabilidadLibrosService _service;

        public ContabilidadLibrosController(IContabilidadLibrosService service)
        {
            _service = service;
        }

        [HttpGet("libro-diario/{idEmpresa}")]
        public async Task<IEnumerable<LibroDiarioLineaDto>> GetLibroDiario(
            int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            return await _service.GetLibroDiarioAsync(idEmpresa, desde, hasta);
        }

        [HttpGet("mayor-general/{idEmpresa}/{idCuentaContable}")]
        public async Task<ActionResult<MayorGeneralResumenDto>> GetMayorGeneral(
            int idEmpresa,
            int idCuentaContable,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            var resultado = await _service.GetMayorGeneralAsync(idEmpresa, idCuentaContable, desde, hasta);
            if (resultado == null)
                return NotFound();

            return resultado;
        }

        [HttpGet("mayor-general-resumen/{idEmpresa}")]
        public async Task<IEnumerable<MayorGeneralResumenDto>> GetMayorGeneralResumen(
            int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            return await _service.GetMayorGeneralResumenAsync(idEmpresa, desde, hasta);
        }
    }
}
