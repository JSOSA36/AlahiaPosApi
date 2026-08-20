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

        /// <summary>
        /// Con ?nav=1 (apertura desde POS HTTPS) devolver HTML que se cierra solo.
        /// Así la impresión no depende de XHR bloqueado por Mixed Content.
        /// </summary>
        private IActionResult PrintOk(object payload)
        {
            if (Request.Query.ContainsKey("nav"))
            {
                return Content(
                    "<!DOCTYPE html><html><head><meta charset=\"utf-8\">" +
                    "<title>Alahia Print</title></head>" +
                    "<body style=\"font-family:sans-serif;padding:12px;font-size:14px\">" +
                    "Impresión enviada a la impresora." +
                    "<script>setTimeout(function(){try{window.close();}catch(e){}},500);</script>" +
                    "</body></html>",
                    "text/html");
            }

            return Ok(payload);
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

                return PrintOk(new
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

                return PrintOk(new
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
        // 🔹 PREVIEW TICKET (PDF 80mm + QR)
        // ============================

        [HttpGet("factura-preview/{idFactura}")]
        public async Task<IActionResult> PreviewFactura(int idFactura)
        {
            if (idFactura <= 0)
                return BadRequest("IdFactura inválido");

            try
            {
                var pdf = await _printer.PreviewTicketFacturaClientePdfAsync(idFactura);
                return File(pdf, "application/pdf", $"ticket-preview-{idFactura}.pdf");
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

                return PrintOk(new
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

                return PrintOk(new
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

        [HttpGet("nota-credito/{idNotaCredito}/{idEmpresa}")]
        public async Task<IActionResult> PrintNotaCredito(
            int idNotaCredito,
            int idEmpresa)
        {
            if (idNotaCredito <= 0)
            {
                return BadRequest("IdNotaCredito inválido");
            }

            if (idEmpresa <= 0)
            {
                return BadRequest("IdEmpresa inválido");
            }

            try
            {
                await _printer.GenerateTicketNotaCredito(
                    idNotaCredito,
                    idEmpresa);

                return PrintOk(new
                {
                    success = true,
                    message = "Nota de crédito enviada a imprimir"
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

        /// <summary>Recibo de abono CxC (cliente).</summary>
        [HttpGet("recibo-abono/{idPago}")]
        public async Task<IActionResult> PrintReciboAbono(int idPago)
        {
            if (idPago <= 0)
                return BadRequest("IdPago inválido");

            try
            {
                await _printer.GenerateTicketReciboAbono(idPago);
                return PrintOk(new
                {
                    success = true,
                    message = "Recibo de abono enviado a imprimir"
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

                return PrintOk(new
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
        // 🔹 TEST / VERSION API
        // ============================

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(global::PrinterApi.PrinterAgentInfo.BuildStatusPayload("Printer API funcionando"));
        }

        [HttpGet("version")]
        public IActionResult Version()
        {
            return Ok(global::PrinterApi.PrinterAgentInfo.BuildStatusPayload());
        }

        // ============================
        // 🔹 IMPRESORAS / CONFIG LOCAL
        // ============================

        [HttpGet("impresoras")]
        public IActionResult ListarImpresoras([FromServices] global::PrinterApi.Servicios.IPrinterLocalSettings settings)
        {
            var snap = settings.GetSnapshot();
            return Ok(new
            {
                success = true,
                factura = snap.Factura,
                lavador = snap.Lavador,
                localConfigPath = snap.LocalConfigPath,
                printers = snap.InstalledPrinters
            });
        }

        [HttpGet("settings")]
        public IActionResult GetSettings([FromServices] global::PrinterApi.Servicios.IPrinterLocalSettings settings)
        {
            var snap = settings.GetSnapshot();
            return Ok(new
            {
                success = true,
                factura = snap.Factura,
                lavador = snap.Lavador,
                localConfigPath = snap.LocalConfigPath,
                printers = snap.InstalledPrinters
            });
        }

        public class SavePrinterSettingsRequest
        {
            public string? Factura { get; set; }
            public string? Lavador { get; set; }
        }

        [HttpPut("settings")]
        public IActionResult SaveSettings(
            [FromBody] SavePrinterSettingsRequest body,
            [FromServices] global::PrinterApi.Servicios.IPrinterLocalSettings settings)
        {
            try
            {
                var snap = settings.Save(body?.Factura ?? "", body?.Lavador);
                return Ok(new
                {
                    success = true,
                    message = "Impresora guardada",
                    factura = snap.Factura,
                    lavador = snap.Lavador,
                    localConfigPath = snap.LocalConfigPath,
                    printers = snap.InstalledPrinters
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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

                return PrintOk(new
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