using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReporteVentaController : ControllerBase
    {
        private readonly IReporteVentaService _reporte;
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public ReporteVentaController(
            IReporteVentaService reporte,
            ISucursalService sucursales,
            ISesionTokenResolver tokens)
        {
            _reporte = reporte;
            _sucursales = sucursales;
            _tokens = tokens;
        }

        [HttpGet("facturas")]
        public async Task<IActionResult> Facturas(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idSucursalFiltro = null)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest(new { message = "Empresa inválida." });
            }

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var result = await _reporte.ObtenerFacturasAsync(idEmpresa, desde, hasta, scope);
            return Ok(result);
        }

        [HttpGet("productos")]
        public async Task<IActionResult> Productos(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idSucursalFiltro = null)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest(new { message = "Empresa inválida." });
            }

            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var result = await _reporte.ObtenerProductosAsync(idEmpresa, desde, hasta, scope);
            return Ok(result);
        }
    }
}
