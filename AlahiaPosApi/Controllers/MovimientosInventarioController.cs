using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
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

        // ======================================================
        // 🔥 CONSTRUCTOR
        // ======================================================

        public MovimientosInventarioController(

            IMapper mapper,

            IMovimientosInventarioService
                movimientosInventario,

            IProductos productos
        )
        {

            _Mapper = mapper;

            _movimientosInventario =
                movimientosInventario;

            _productos =
                productos;
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
                .Listar(idEmpresa);
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

            int? idProducto
        )
        {

            try
            {

                var result =
                    await _movimientosInventario
                    .FiltrarHistorial(

                        idEmpresa,

                        desde,

                        hasta,

                        tipoMovimiento,

                        motivo,

                        idUsuario,

                        idProducto
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

                var result =
                    await _movimientosInventario
                    .Eliminar(id);

                if (!result)
                {
                    return NotFound(
                        "Movimiento no encontrado");
                }

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

                        hasta
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

                        hasta
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
    }
}