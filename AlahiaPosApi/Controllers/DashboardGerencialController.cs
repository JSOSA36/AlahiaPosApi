using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardGerencialController : ControllerBase
    {
        private readonly IDashboardGerencialService _service;
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public DashboardGerencialController(
            IDashboardGerencialService service,
            ISucursalService sucursales,
            ISesionTokenResolver tokens)
        {
            _service = service;
            _sucursales = sucursales;
            _tokens = tokens;
        }

        /// <summary>
        /// Panel gerencial del mes en curso.
        /// idSucursalFiltro: 0/omitido = todas las sucursales a las que el usuario tiene acceso.
        /// </summary>
        [HttpGet("{idEmpresa:int}")]
        public async Task<IActionResult> ObtenerMesActual(int idEmpresa, int? idSucursalFiltro = null)
        {
            if (idEmpresa <= 0)
                return BadRequest("IdEmpresa inválido.");

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            try
            {
                var result = await _service.ObtenerMesActualAsync(idEmpresa, scope);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "No se pudo cargar el Panel Gerencial.",
                    detalle = ex.GetBaseException().Message
                });
            }
        }
    }
}
