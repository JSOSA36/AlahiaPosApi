using Microsoft.AspNetCore.Mvc;
using PrinterApi.Interfaz;

namespace PrinterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrinterController : ControllerBase
    {
        private readonly IPrinterTicket _printer;

        public PrinterController(IPrinterTicket printer)
        {
            _printer = printer;
        }

        // ============================
        // 🔹 PRINT FACTURA CLIENTE
        // ============================
        [HttpGet("factura/{idFactura}")]
        public async Task<IActionResult> PrintFactura(int idFactura)
        {
            if (idFactura <= 0)
                return BadRequest("IdFactura inválido");

            try
            {
                await _printer.GenerateTicketFacturaCliente(idFactura);

                return Ok(new
                {
                    success = true,
                    message = "Factura enviada a imprimir"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================
        // 🔹 PRINT TICKET LAVADOR
        // ============================
        [HttpGet("lavador/{idFacturaHeader}")]
        public async Task<IActionResult> PrintLavador(int idFacturaHeader)
        {
            if (idFacturaHeader <= 0)
                return BadRequest("IdFacturaHeader inválido");

            try
            {
                await _printer.GenerateTicketLavador(idFacturaHeader);

                return Ok(new
                {
                    success = true,
                    message = "Tickets de lavador enviados"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ============================
        // 🔹 TEST API
        // ============================
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                success = true,
                message = "Printer API funcionando 🔥"
            });
        }
    }
}