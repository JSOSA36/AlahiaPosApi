using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaCierreController
        : ControllerBase
    {

        ICajaCierreService _service;
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;
        private readonly IReporteCruceStockService _cruceStock;

        public CajaCierreController(
            ICajaCierreService service,
            ISucursalService sucursales,
            ISesionTokenResolver tokens,
            IReporteCruceStockService cruceStock
        )
        {
            _service = service;
            _sucursales = sucursales;
            _tokens = tokens;
            _cruceStock = cruceStock;
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        [HttpGet]
        public async Task<IEnumerable<CajaListadoDto>>
            Get()
        {
            return await _service
                .GetAllAsync();
        }
        /* =====================================
🔥 GET BY FECHA
===================================== */

        [HttpGet]
        [Route("GetByFecha")]
        public async Task<IActionResult> GetByFecha(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var listado = await _service.GetByFechaAsync(idEmpresa, desde, hasta);
            var filtrado = listado
                .Where(x => scope.Incluye(x.IdSucursal))
                .ToList();

            foreach (var item in filtrado)
            {
                var id = item.IdSucursal is > 0 ? item.IdSucursal.Value : scope.IdPrincipal;
                item.NombreSucursal = scope.Sucursales
                    .FirstOrDefault(s => s.IdSucursal == id)?.Nombre;
            }

            return Ok(filtrado);
        }

        /* =====================================
        🔥 CRUCE STOCK vs VENTAS POR TURNO
        ===================================== */

        [HttpGet]
        [Route("CruceStockVsVentas")]
        public async Task<IActionResult> CruceStockVsVentas(
            int idEmpresa,
            DateTime desde,
            DateTime hasta,
            int? idProducto = null,
            int? idUsuario = null,
            int? idCajaCierre = null)
        {
            var lineas = await _cruceStock.ObtenerCruceAsync(
                idEmpresa,
                desde,
                hasta,
                idProducto,
                idUsuario,
                idCajaCierre);

            return Ok(lineas);
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        [HttpGet("{id}")]
        public async Task<CajaCierre?>
            GetById(int id)
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 ÚLTIMO CIERRE
        ===================================== */

        [HttpGet]
        [Route("UltimoCierre")]
        public async Task<CajaCierre?>
            UltimoCierre(
                int idEmpresa,
                int idUsuario = 0
            )
        {
            var uid = await IdUsuarioCobroResolver.ResolverAsync(
                HttpContext,
                _tokens,
                idUsuario);

            if (uid <= 0)
            {
                return null;
            }

            return await _service
                .GetUltimoCierreAsync(
                    idEmpresa,
                    uid
                );
        }

        /* =====================================
        🔥 PROCESAR CIERRE
        ===================================== */

        [HttpPost]
        [Route("Procesar")]
        [RequiereTerminalPos]
        public async Task<IActionResult>
Procesar(

    [FromBody]
    CajaCierre model
)
        {

            if (model == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Modelo inválido"
                });
            }

            var cierre =

                await _service
                .ProcesarCierreAsync(
                    model
                );

            return Ok(new
            {
                success = true,
                message = "Caja cerrada correctamente",
                data = cierre
            });
        }


        [HttpGet]
        [Route("ImprimirCierre/{idCajaCierre}")]
        public async Task<IActionResult>
 ImprimirCierre(

     int idCajaCierre
 )
        {
            try
            {
                var cierre =

                    await _service
                    .ImprimirCierre(

                        idCajaCierre
                    );

                if (cierre == null)
                {
                    return NotFound(
                        new
                        {
                            success = false,
                            message = "Cierre no encontrado",
                            data = cierre
                        }
                    );
                }

                return Ok(cierre);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = ex.Message
                    }
                );
            }
        }
        /* =====================================
        🔥 DELETE
        ===================================== */

        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {
            await _service
                .DeleteAsync(id);
        }
    }
}