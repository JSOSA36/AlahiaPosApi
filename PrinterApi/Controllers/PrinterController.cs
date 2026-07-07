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
        public async Task<IActionResult>
        PrintFactura(int idFactura)
        {
            if (idFactura <= 0)
                return BadRequest(
                    "IdFactura inválido"
                );

            try
            {
                await _printer
                .GenerateTicketFacturaCliente(
                    idFactura
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Factura enviada a imprimir"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }

        // ============================
        // 🔹 PRINT FACTURA PDF
        // ============================

        [HttpGet("facturaPDF/{idFactura}")]
        public async Task<IActionResult>
        PrintFacturaPDF(int idFactura)
        {
            if (idFactura <= 0)
                return BadRequest(
                    "IdFactura inválido"
                );

            try
            {
                await _printer
                .GenerateFactDirect(
                    idFactura
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Factura enviada a imprimir"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }

        // ============================
        // 🔹 PRINT TICKET LAVADOR
        // ============================

        [HttpGet("lavador/{idFacturaHeader}")]
        public async Task<IActionResult>
        PrintLavador(int idFacturaHeader)
        {
            if (idFacturaHeader <= 0)
                return BadRequest(
                    "IdFacturaHeader inválido"
                );

            try
            {
                await _printer
                .GenerateTicketLavador(
                    idFacturaHeader
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Tickets de lavador enviados"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }

        // ============================
        // 🔹 PRINT ENCARGO BIZCOCHO
        // ============================

        [HttpGet("ticket/{idFacturaHeader}/{idEmpresa}")]
        public async Task<IActionResult>
        PrintBizcocho(

            int idFacturaHeader,

            int idEmpresa
        )
        {
            if (idFacturaHeader <= 0)
                return BadRequest(
                    "IdFacturaHeader inválido"
                );

            if (idEmpresa <= 0)
                return BadRequest(
                    "IdEmpresa inválido"
                );

            try
            {
                await _printer
                .GenerateTicketBizcocho(

                    idFacturaHeader,

                    idEmpresa
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Ticket bizcocho generado"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }

        // ============================
        // 🔹 PRINT CIERRE CAJA
        // ============================

        [HttpGet("cierre/{idCajaCierre}")]
        public async Task<IActionResult>
        PrintCierre(

            int idCajaCierre
        )
        {
            if (idCajaCierre <= 0)
                return BadRequest(
                    "IdCajaCierre inválido"
                );

            try
            {
                await _printer
                .ImprimirCierre(

                    idCajaCierre
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Cierre enviado a imprimir"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
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

                message =
                    "Printer API funcionando 🔥"
            });
        }
        // ============================
        // 🔹 PRINT CIERRE ENCARGOS
        // ============================

        [HttpGet("cierre-encargos/{idEmpresa}")]
        public async Task<IActionResult>
        PrintCierreEncargos(

            int idEmpresa
        )
        {
            if (idEmpresa <= 0)
                return BadRequest(
                    "IdEmpresa inválido"
                );

            try
            {
                await _printer
                .ImprimirCierreEncargos(

                    idEmpresa
                );

                return Ok(new
                {
                    success = true,

                    message =
                        "Cierre de encargos enviado a imprimir"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }
    }
}