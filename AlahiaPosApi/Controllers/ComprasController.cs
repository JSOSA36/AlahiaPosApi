using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private readonly IComprasService _comprasService;

        public ComprasController(IComprasService comprasService)
        {
            _comprasService = comprasService;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IActionResult> Listar(
            int idEmpresa,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var result = await _comprasService.ListarAsync(idEmpresa, estado, desde, hasta);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Detalle/{id}/{idEmpresa}")]
        public async Task<IActionResult> Obtener(int id, int idEmpresa)
        {
            var result = await _comprasService.ObtenerPorIdAsync(id, idEmpresa);
            if (result == null)
                return NotFound(new { message = "Factura de compra no encontrada." });

            return Ok(result);
        }

        [HttpGet("Pendientes/{idEmpresa}")]
        public async Task<IActionResult> Pendientes(
            int idEmpresa,
            [FromQuery] int? idProveedor = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var result = await _comprasService.ListarPendientesAsync(
                    idEmpresa,
                    idProveedor,
                    desde,
                    hasta);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Borrador")]
        public async Task<IActionResult> GuardarBorrador([FromBody] GuardarFacturaCompraRequest request)
        {
            try
            {
                var result = await _comprasService.GuardarBorradorAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPost("{id}/Confirmar")]
        public async Task<IActionResult> Confirmar(int id, [FromBody] ConfirmarFacturaCompraRequest request)
        {
            try
            {
                var result = await _comprasService.ConfirmarAsync(id, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPost("{id}/Pago")]
        public async Task<IActionResult> RegistrarPago(int id, [FromBody] RegistrarPagoProveedorRequest request)
        {
            try
            {
                var result = await _comprasService.RegistrarPagoAsync(id, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpGet("{id}/Pagos/{idEmpresa}")]
        public async Task<IActionResult> ObtenerPagos(int id, int idEmpresa)
        {
            var pagos = await _comprasService.ObtenerPagosAsync(id, idEmpresa);
            return Ok(pagos);
        }

        [HttpPut("{id}/Anular/{idEmpresa}")]
        public async Task<IActionResult> Anular(int id, int idEmpresa)
        {
            try
            {
                await _comprasService.AnularAsync(id, idEmpresa);
                return Ok(new { success = true, message = "Factura anulada." });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        /// <summary>Facturas con recepción física pendiente/parcial (Almacén).</summary>
        [HttpGet("PendientesRecepcion/{idEmpresa}")]
        public async Task<IActionResult> PendientesRecepcion(
            int idEmpresa,
            [FromQuery] string? texto = null,
            [FromQuery] int? idProveedor = null)
        {
            var result = await _comprasService.BuscarPendientesRecepcionAsync(idEmpresa, texto, idProveedor);
            return Ok(result);
        }

        [HttpGet("Recepcion/{id}/{idEmpresa}")]
        public async Task<IActionResult> ObtenerParaRecepcion(int id, int idEmpresa)
        {
            var result = await _comprasService.ObtenerParaRecepcionAsync(id, idEmpresa);
            if (result == null)
                return NotFound(new { message = "Factura de compra no encontrada." });

            return Ok(result);
        }

        [HttpPost("{id}/Recepcion")]
        public async Task<IActionResult> ConfirmarRecepcion(int id, [FromBody] ConfirmarRecepcionCompraRequest request)
        {
            try
            {
                var result = await _comprasService.ConfirmarRecepcionAsync(id, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        // ========== Órdenes de compra (tipo 5) ==========

        [HttpGet("Ordenes/{idEmpresa}")]
        public async Task<IActionResult> ListarOrdenes(
            int idEmpresa,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var result = await _comprasService.ListarOrdenesAsync(idEmpresa, estado, desde, hasta);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Ordenes/Borrador")]
        public async Task<IActionResult> GuardarBorradorOrden([FromBody] GuardarFacturaCompraRequest request)
        {
            try
            {
                var result = await _comprasService.GuardarBorradorOrdenAsync(request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPost("Ordenes/{id}/Emitir")]
        public async Task<IActionResult> EmitirOrden(int id, [FromBody] EmitirOrdenCompraRequest request)
        {
            try
            {
                var result = await _comprasService.EmitirOrdenAsync(id, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPost("Ordenes/{id}/Enviar")]
        public async Task<IActionResult> EnviarOrden(int id, [FromBody] EnviarOrdenCompraRequest request)
        {
            try
            {
                var result = await _comprasService.MarcarOrdenEnviadaAsync(id, request);
                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Orden marcada como enviada. El envío SMTP automático se habilitará en la siguiente fase."
                });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        [HttpPost("Ordenes/{id}/GenerarFactura/{idEmpresa}")]
        public async Task<IActionResult> GenerarFacturaDesdeOrden(int id, int idEmpresa, [FromQuery] int idUsuario = 0)
        {
            try
            {
                var result = await _comprasService.GenerarFacturaDesdeOrdenAsync(id, idEmpresa, idUsuario);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        /// <summary>Estado de cuenta / auxiliar del proveedor (FACTC + pagos).</summary>
        [HttpGet("EstadoCuenta/{idEmpresa}/{idProveedor}")]
        public async Task<IActionResult> EstadoCuenta(
            int idEmpresa,
            int idProveedor,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var d = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
                var h = (hasta ?? DateTime.Today).Date;
                var result = await _comprasService.ObtenerEstadoCuentaProveedorAsync(
                    idEmpresa, idProveedor, d, h);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        /// <summary>Análisis producto×proveedor desde historial FACTC (sin ProductoProveedor).</summary>
        [HttpGet("AnalisisProducto/{idEmpresa}")]
        public async Task<IActionResult> AnalisisProducto(
            int idEmpresa,
            [FromQuery] int? idProducto = null,
            [FromQuery] int? idProveedor = null,
            [FromQuery] int? idAlmacen = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            try
            {
                var result = await _comprasService.ObtenerAnalisisProductoProveedorAsync(
                    new AnalisisCompraFiltroRequest
                    {
                        IdEmpresa = idEmpresa,
                        IdProducto = idProducto,
                        IdProveedor = idProveedor,
                        IdAlmacen = idAlmacen,
                        Desde = desde,
                        Hasta = hasta
                    });
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }

        /// <summary>Formato 606 DGII desde FACTC confirmadas del período.</summary>
        [HttpGet("Reporte606/{idEmpresa}")]
        public async Task<IActionResult> Reporte606(
            int idEmpresa,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] string? periodo = null)
        {
            try
            {
                var d = (desde ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
                var h = (hasta ?? DateTime.Today).Date;
                var result = await _comprasService.ObtenerReporte606Async(idEmpresa, d, h, periodo);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { success = false, message });
            }
        }
    }
}
