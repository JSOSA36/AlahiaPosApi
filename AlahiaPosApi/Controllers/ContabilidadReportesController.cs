using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContabilidadReportesController : ControllerBase
    {
        private readonly IContabilidadReportesService _service;

        public ContabilidadReportesController(IContabilidadReportesService service)
        {
            _service = service;
        }

        [HttpGet("balance-comprobacion/{idEmpresa}")]
        public async Task<BalanceComprobacionResumenDto> GetBalanceComprobacion(
            int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            return await _service.GetBalanceComprobacionAsync(idEmpresa, desde, hasta);
        }

        [HttpGet("estado-resultados/{idEmpresa}")]
        public async Task<EstadoResultadosDto> GetEstadoResultados(
            int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            return await _service.GetEstadoResultadosAsync(idEmpresa, desde, hasta);
        }

        [HttpGet("balance-general/{idEmpresa}")]
        public async Task<BalanceGeneralDto> GetBalanceGeneral(
            int idEmpresa,
            [FromQuery] DateTime fechaCorte)
        {
            return await _service.GetBalanceGeneralAsync(idEmpresa, fechaCorte);
        }
    }
}
