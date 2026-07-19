using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardGerencialController : ControllerBase
    {
        private readonly IDashboardGerencialService _service;

        public DashboardGerencialController(IDashboardGerencialService service)
        {
            _service = service;
        }

        /// <summary>
        /// Panel gerencial del mes en curso (sin filtros de fecha).
        /// </summary>
        [HttpGet("{idEmpresa:int}")]
        public async Task<IActionResult> ObtenerMesActual(int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("IdEmpresa inválido.");

            var result = await _service.ObtenerMesActualAsync(idEmpresa);
            return Ok(result);
        }
    }
}
