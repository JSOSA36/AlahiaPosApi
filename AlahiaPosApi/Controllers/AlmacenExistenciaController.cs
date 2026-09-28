using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenExistenciaController : ControllerBase
    {
        private readonly IAlmacenExistencia _existenciaService;
        private readonly IAlmacenes _almacenes;
        private readonly ISucursalService _sucursales;

        public AlmacenExistenciaController(
            IAlmacenExistencia existenciaService,
            IAlmacenes almacenes,
            ISucursalService sucursales)
        {
            _existenciaService = existenciaService;
            _almacenes = almacenes;
            _sucursales = sucursales;
        }

        [HttpGet("producto/{idProducto}")]
        public async Task<IActionResult> GetPorProducto(
            int idProducto,
            [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest("idEmpresa es obligatorio.");
            }

            var idSucursal = await SucursalConsultaAsync();

            var detalle =
                await _existenciaService.GetDetallePorProducto(
                    idProducto,
                    idEmpresa,
                    idSucursal
                );

            var total =
                await _existenciaService.GetTotalPorProducto(
                    idProducto,
                    idEmpresa,
                    idSucursal
                );

            return Ok(new
            {
                idProducto,
                total,
                detalle
            });
        }

        [HttpGet("{idAlmacen}/{idProducto}")]
        public async Task<IActionResult> GetExistencia(
            int idAlmacen,
            int idProducto,
            [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest("idEmpresa es obligatorio.");
            }

            var almacen = await _almacenes.GetAlmacenById(idAlmacen);
            if (almacen == null || almacen.IdEmpresa != idEmpresa)
            {
                return Ok(new { cantidad = 0m });
            }

            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null
                && almacen.IdSucursal is > 0
                && !await _sucursales.TieneAccesoAsync(
                    sesion.IdUsuario,
                    sesion.IdEmpresa,
                    almacen.IdSucursal.Value))
            {
                return NotFound();
            }

            try
            {
                var existencia =
                    await _existenciaService.GetExistencia(
                        idAlmacen,
                        idProducto,
                        idEmpresa
                    );

                return Ok(new
                {
                    cantidad = existencia?.Cantidad ?? 0
                });
            }
            catch
            {
                return Ok(new { cantidad = 0m });
            }
        }

        private async Task<int?> SucursalConsultaAsync()
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null || sesion.IdSucursal <= 0)
                return null;

            if (!await _sucursales.TieneAccesoAsync(
                sesion.IdUsuario,
                sesion.IdEmpresa,
                sesion.IdSucursal))
            {
                return null;
            }

            return sesion.IdSucursal;
        }
    }
}
