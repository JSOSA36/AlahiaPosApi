using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagoFacturasClientesController : ControllerBase
    {
        private readonly IPagosFacturasClientes _pagosService;

        public PagoFacturasClientesController(IPagosFacturasClientes pagosService)
        {
            _pagosService = pagosService;
        }

        // 🔹 GET: api/PagoFacturasClientes/1
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<PagosFacturasClientes>> GetAll(int IdEmpresa)
        {
            return await _pagosService.GetAllPagosFacturasClientes(IdEmpresa);
        }

        // 🔹 GET: api/PagoFacturasClientes/GetById/5
        [HttpGet("GetById/{IdPago}")]
        public async Task<ActionResult<PagosFacturasClientes>> GetById(int IdPago)
        {
            var pago = await _pagosService.GetPagosFacturasClientesById(IdPago);
            if (pago == null)
                return NotFound($"No se encontró el pago con Id {IdPago}");

            return Ok(pago);
        }

        // 🔹 GET: api/PagoFacturasClientes/GetByFactura/10
        [HttpGet("GetByFactura/{IdFactura}")]
        public async Task<IEnumerable<PagosFacturasClientes>> GetByFactura(int IdFactura)
        {
            return await _pagosService.GetPagosByFacturaId(IdFactura);
        }

        /// <summary>
        /// Estado de cuenta del cliente (auxiliar CxC).
        /// </summary>
        [HttpGet("EstadoCuenta/{idEmpresa:int}/{idCliente:int}")]
        public async Task<IActionResult> EstadoCuenta(
            int idEmpresa,
            int idCliente,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            try
            {
                var result = await _pagosService.ObtenerEstadoCuentaClienteAsync(
                    idEmpresa, idCliente, desde, hasta);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 🔹 POST: api/PagoFacturasClientes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PagosFacturasClientes pago)
        {
            if (pago == null)
                return BadRequest("El pago no puede ser nulo");

            await _pagosService.InsertPagosFacturasClientes(pago);
            return Ok(pago);
        }

        // 🔹 POST: api/PagoFacturasClientes/RegistrarPago/10
        [HttpPost("RegistrarPago/{IdFactura}")]
        public async Task<IActionResult> RegistrarPago(int IdFactura, [FromBody] PagosFacturasClientes pago)
        {
            if (pago == null)
                return BadRequest("Datos del pago inválidos");

            try
            {
                await _pagosService.RegistrarPagoFactura(IdFactura, pago);
                return Ok(new
                {
                    message = "Pago registrado y factura actualizada correctamente",
                    idPago = pago.Id,
                    idFacturaHeader = IdFactura,
                    monto = pago.Monto,
                    formaPago = pago.FormaPago,
                    nota = pago.Nota
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>Datos del recibo de abono para impresión térmica.</summary>
        [HttpGet("recibo/{idPago:int}")]
        public async Task<IActionResult> GetReciboAbono(int idPago)
        {
            if (idPago <= 0)
                return BadRequest(new { message = "IdPago inválido" });

            var recibo = await _pagosService.GetReciboAbonoByPagoIdAsync(idPago);
            if (recibo == null)
                return NotFound(new { message = "Pago no encontrado" });

            return Ok(recibo);
        }

        /// <summary>
        /// Cobro a varias facturas del mismo cliente (una transacción).
        /// </summary>
        [HttpPost("RegistrarPagoLote")]
        public async Task<IActionResult> RegistrarPagoLote([FromBody] RegistrarPagoLoteRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Datos del pago inválidos" });

            try
            {
                var result = await _pagosService.RegistrarPagoLoteAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 🔹 PUT: api/PagoFacturasClientes/5
        [HttpPut("{IdFacturaHeader}")]
        public async Task<IActionResult> Put(int IdFacturaHeader, [FromBody] PagosFacturasClientes pago)
        {
            if (pago == null)
                return BadRequest("Datos inválidos");

            try
            {
                await _pagosService.RegistrarPagoFactura(IdFacturaHeader, pago);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        // 🔹 DELETE: api/PagoFacturasClientes/5
        [HttpDelete("{IdPago}")]
        public IActionResult Delete(int IdPago)
        {
            _pagosService.DeletePagosFacturasClientes(IdPago);
            return NoContent();
        }
    }
}
