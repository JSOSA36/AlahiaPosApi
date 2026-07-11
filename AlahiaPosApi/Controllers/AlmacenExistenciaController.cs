using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenExistenciaController : ControllerBase
    {
        private readonly IAlmacenExistencia _existenciaService;

        public AlmacenExistenciaController(
            IAlmacenExistencia existenciaService)
        {
            _existenciaService = existenciaService;
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

            var detalle =
                await _existenciaService.GetDetallePorProducto(
                    idProducto,
                    idEmpresa
                );

            var total =
                await _existenciaService.GetTotalPorProducto(
                    idProducto,
                    idEmpresa
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
    }
}
