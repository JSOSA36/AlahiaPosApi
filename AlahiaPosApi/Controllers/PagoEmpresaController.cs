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
                    return BadRequest("Debe subir un comprobante.");

                string archivoUrl;

                using (var ms = new MemoryStream())
                {
                    await dto.Imagen.CopyToAsync(ms);

                    archivoUrl = Utility.UploadFileFtp(
                        ms.ToArray(),
                        Guid.NewGuid() + Path.GetExtension(dto.Imagen.FileName)
                    );
                }

                dto.ArchivoUrl = archivoUrl;

                await _pagoService.CrearPagoAsync(dto);

                return Ok(new
                {
                    message = "Pago enviado correctamente ⏳. Pendiente de validación.",
                    archivo = archivoUrl
                });
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