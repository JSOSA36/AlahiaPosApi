using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientosInventarioController : ControllerBase
    {

        // ======================================================
        // 🔥 SERVICES
        // ======================================================

        IMapper _Mapper;

        IMovimientosInventarioService
            _movimientosInventario;

        IProductos
            _productos;

        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public MovimientosInventarioController(

            IMapper mapper,

            IMovimientosInventarioService
                movimientosInventario,

            IProductos productos,

            ISucursalService sucursales,

            ISesionTokenResolver tokens
        )
        {

            _Mapper = mapper;

            _movimientosInventario =
                movimientosInventario;

            _productos =
                productos;

            _sucursales = sucursales;
            _tokens = tokens;
        }

        // ======================================================
        // 🔥 LISTAR MOVIMIENTOS
        // ======================================================

        [HttpGet]
        public async Task<IEnumerable<MovimientosInventario>>
            Get(int idEmpresa)
        {

            return await
                _movimientosInventario
                .Listar(idEmpresa, SucursalSesion());
        }
        // ======================================================
        // 🔥 FILTRAR HISTORIAL
        // ======================================================

        [HttpGet]
        [Route("FiltrarHistorial")]
        public async Task<IActionResult>
            FiltrarHistorial(

            int idEmpresa,

            DateTime? desde,

            DateTime? hasta,

            string? tipoMovimiento,

            string? motivo,

            int? idUsuario,

            int? idProducto,

            int? idSucursalFiltro = null
        )
        {

            try
            {
                var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                    HttpContext, _tokens, _sucursales, idEmpresa, idSucursalFiltro);
                if (error != null)
                    return error;

                var result =
                    await _movimientosInventario
                    .FiltrarHistorial(

                        idEmpresa,

                        desde,

                        hasta,

                        tipoMovimiento,

                        motivo,

                        idUsuario,

                        idProducto,

                        scope.IdsPermitidos,
                        scope.IdPrincipal
                    );

                foreach (var row in result)
                {
                    var id = row.IdSucursal is > 0 ? row.IdSucursal.Value : scope.IdPrincipal;
                    row.NombreSucursal = scope.Sucursales
                        .FirstOrDefault(s => s.IdSucursal == id)?.Nombre ?? "";
                }

                return Ok(result);
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }
        // ======================================================
        // 🔥 OBTENER POR ID
        // ======================================================

        [HttpGet]
        [Route("GetById/{id}")]
        public async Task<IActionResult>
            GetById(int id)
        {

            var result =
                await _movimientosInventario
                .ObtenerPorId(id);

            if (result == null)
            {
                return NotFound(
                    "Movimiento no encontrado");
            }

            var otra = TenantRecurso.RechazarSiOtraEmpresa(HttpContext, result.IdEmpresa);
            if (otra != null) return otra;

            return Ok(result);
        }

        // ======================================================
        // 🔥 GUARDAR MOVIMIENTO
        // ======================================================

        [HttpPost]
        [Route("GuardarMovimiento")]
        public async Task<IActionResult>
            GuardarMovimiento(

            [FromBody]
            MovimientosInventario value
        )
        {

            try
            {

                // =============================================
                // 🔥 VALIDAR
                // =============================================

                if (
                    value == null ||

                    value.Detalles == null ||

                    !value.Detalles.Any()
                )
                {
                    return BadRequest(
                        "Debe agregar productos");
                }

                // =============================================
                // 🔥 FECHA
                // =============================================

                value.Fecha =
                    DateTime.Now;

                var sesion = SesionHttp.TryGet(HttpContext);
                if (sesion != null)
                {
                    value.IdEmpresa = sesion.IdEmpresa;
                    value.IdUsuario = sesion.IdUsuario;
                    if (value.IdSucursal is null or <= 0 && sesion.IdSucursal > 0)
                        value.IdSucursal = sesion.IdSucursal;
                }

                // =============================================
                // 🔥 GUARDAR
                // =============================================
                
                var result =
                    await _movimientosInventario
                    .GuardarMovimiento(value);

                return Ok(new
                {
                    message =
                        "Movimiento guardado correctamente",

                    idMovimiento =
                        result.Id
                });
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 ELIMINAR MOVIMIENTO
        // ======================================================

        [HttpDelete]
        [Route("{id}")]
        public async Task<IActionResult>
            Delete(int id)
        {

            try
            {

                var existente =
                    await _movimientosInventario
                    .ObtenerPorId(id);

                if (existente == null)
                {
                    return NotFound(
                        "Movimiento no encontrado");
                }

                var otra = TenantRecurso.RechazarSiOtraEmpresa(
                    HttpContext,
                    existente.IdEmpresa);
                if (otra != null) return otra;

                var result =
                    await _movimientosInventario
                    .Eliminar(id);

                return Ok(new
                {
                    message =
                        "Movimiento eliminado correctamente"
                });
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 FILTRAR POR FECHA
        // ======================================================

        [HttpGet]
        [Route("FiltrarPorFecha")]
        public async Task<IActionResult>
            FiltrarPorFecha(

            int idEmpresa,

            DateTime desde,

            DateTime hasta
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .FiltrarPorFecha(

                        idEmpresa,

                        desde,

                        hasta,

                        SucursalSesion()
                    );

                return Ok(result);
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 VALIDAR STOCK
        // ======================================================

        [HttpGet]
        [Route("ValidarStock")]
        public async Task<IActionResult>
            ValidarStock(

            int idProducto,

            decimal cantidad
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .ValidarStock(

                        idProducto,

                        cantidad
                    );

                return Ok(result);
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 KARDEX
        // ======================================================

        [HttpGet]
        [Route("KardexProducto")]
        public async Task<IActionResult>
            KardexProducto(

            int idProducto,

            DateTime? desde,

            DateTime? hasta
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .KardexProducto(

                        idProducto,

                        desde,

                        hasta,

                        SucursalSesion()
                    );

                return Ok(result);
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 STOCK BAJO
        // ======================================================

        [HttpGet]
        [Route("ProductosStockBajo")]
        public async Task<IActionResult>
            ProductosStockBajo(
            int idEmpresa
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .ProductosStockBajo(
                        idEmpresa
                    );

                return Ok(result);
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 UPDATE STOCK MANUAL
        // ======================================================

        [HttpPut]
        [Route("ActualizarStock")]
        public async Task<IActionResult>
            ActualizarStock(

            int idProducto,

            decimal nuevoStock
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .ActualizarStockProducto(

                        idProducto,

                        nuevoStock
                    );

                if (!result)
                {
                    return NotFound(
                        "Producto no encontrado");
                }

                return Ok(new
                {
                    message =
                        "Stock actualizado correctamente"
                });
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
            }
        }

        private int? SucursalSesion()
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            return sesion != null && sesion.IdSucursal > 0
                ? sesion.IdSucursal
                : null;
        }
    }
}