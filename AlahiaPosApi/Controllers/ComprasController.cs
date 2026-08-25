using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Servicios;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComprasController : ControllerBase
    {
        private static readonly HashSet<string> ImagenMimes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif"
        };

        private static readonly HashSet<string> PdfMimes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf", "application/x-pdf"
        };

        private readonly IComprasService _comprasService;
        private readonly IFacturaCompraImagenService _imagenService;

        public ComprasController(
            IComprasService comprasService,
            IFacturaCompraImagenService imagenService)
        {
            _comprasService = comprasService;
            _imagenService = imagenService;
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

        /// <summary>
        /// Lee una foto o PDF de factura de proveedor y propone encabezado + líneas.
        /// No graba: el usuario revisa y guarda el borrador.
        /// </summary>
        [HttpPost("InterpretarImagen")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 12 * 1024 * 1024)]
        public async Task<IActionResult> InterpretarImagen(
            [FromForm] int idEmpresa,
            [FromForm] int idUsuario,
            IFormFile? archivo,
            IFormFile? imagen)
        {
            try
            {
                var file = archivo ?? imagen;
                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Adjunte una foto o un PDF de la factura." });

                await using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var mime = (file.ContentType ?? "").Split(';')[0].Trim();
                var ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
                var esPdf = PdfMimes.Contains(mime) || ext == ".pdf" || EsPdf(bytes);
                var extImgOk = ext is ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif";

                FacturaCompraInterpretarRequest request;
                if (esPdf)
                {
                    request = FacturaCompraPdfReader.Preparar(idEmpresa, idUsuario, bytes);
                }
                else if (ImagenMimes.Contains(mime) || extImgOk)
                {
                    request = new FacturaCompraInterpretarRequest
                    {
                        IdEmpresa = idEmpresa,
                        IdUsuario = idUsuario,
                        Paginas =
                        {
                            new FacturaCompraInterpretarPagina
                            {
                                Bytes = bytes,
                                Mime = string.IsNullOrWhiteSpace(mime) ? "image/jpeg" : mime
                            }
                        }
                    };
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Use una foto JPG/PNG/WEBP o un PDF de la factura."
                    });
                }

                var result = await _imagenService.InterpretarAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(message)
                        ? "No se pudo leer el archivo."
                        : message
                });
            }
        }

        private static bool EsPdf(byte[] bytes) =>
            bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;
    }
}
