using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagoEmpresaController : ControllerBase
    {
        private readonly IPagoEmpresaService _pagoService;

        public PagoEmpresaController(IPagoEmpresaService pagoService)
        {
            _pagoService = pagoService;
        }

        // =====================================================
        // 🔹 SUBIR VOUCHER (CLIENTE)
        // =====================================================
        [HttpPost("SubirPago")]
        public async Task<IActionResult> SubirPago([FromForm] CrearPagoDto dto)
        {
            try
            {
                if (dto.Imagen == null || dto.Imagen.Length == 0)
                    return BadRequest(new { message = "Debe subir un comprobante." });

                await using var ms = new MemoryStream();
                await dto.Imagen.CopyToAsync(ms);
                dto.ImagenBytes = ms.ToArray();
                dto.ImagenContentType = dto.Imagen.ContentType;
                dto.ImagenFileName = dto.Imagen.FileName;

                await _pagoService.CrearPagoAsync(dto);

                return Ok(new
                {
                    message = "Pago enviado correctamente ⏳. Pendiente de validación.",
                    archivo = dto.ArchivoUrl
                });
            }
            catch (MontoVoucherInsuficienteException ex)
            {
                return BadRequest(new
                {
                    codigo = "MONTO_INSUFICIENTE",
                    message = ex.Message,
                    montoVoucherDop = ex.MontoVoucherDop,
                    totalAdeudadoDop = ex.TotalAdeudadoDop,
                    montoReconexionDop = ex.MontoReconexionDop,
                    montoPlanDop = ex.MontoPlanDop,
                    faltanteDop = Math.Round(ex.TotalAdeudadoDop - ex.MontoVoucherDop, 2)
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al registrar el pago ❌",
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // 🔹 LISTAR PAGOS (ADMIN)
        // =====================================================
        [HttpGet("ObtenerPagos")]
        public async Task<IActionResult> ObtenerPagos()
        {
            try
            {
                var pagos = await _pagoService.ObtenerPagosAsync();

                return Ok(pagos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener los pagos ❌",
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // 🔹 HISTORIAL DE PAGOS (CLIENTE — su empresa)
        // =====================================================
        [HttpGet("ObtenerPagosPorEmpresa/{idEmpresa:int}")]
        public async Task<IActionResult> ObtenerPagosPorEmpresa(int idEmpresa)
        {
            try
            {
                if (idEmpresa <= 0)
                    return BadRequest("IdEmpresa inválido.");

                var pagos = await _pagoService.ObtenerPagosPorEmpresaAsync(idEmpresa);
                return Ok(pagos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al obtener el historial de pagos",
                    message = ex.Message
                });
            }
        }

        // =====================================================
        // 🔹 APROBAR / RECHAZAR PAGO (ADMIN)
        // =====================================================
        [HttpPost("ValidarPago")]
        public async Task<IActionResult> ValidarPago([FromBody] ValidarPagoDto dto)
        {
            try
            {
                await _pagoService.ValidarPagoAsync(dto);

                return Ok(new
                {
                    message = dto.Estado == "APROBADO"
                        ? "Pago aprobado y servicio activado ✅"
                        : "Pago rechazado ❌"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Error al validar el pago ❌",
                    message = ex.Message
                });
            }
        }
    }
}
