using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
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

            await _pagosService.RegistrarPagoFactura(IdFactura, pago);
            return Ok(new { message = "Pago registrado y factura actualizada correctamente" });
        }

        // 🔹 PUT: api/PagoFacturasClientes/5
        [HttpPut("{IdFacturaHeader}")]
        public IActionResult Put(int IdFacturaHeader, [FromBody] PagosFacturasClientes pago)
        {
            if (pago == null)
                return BadRequest("Datos inválidos");

            _pagosService.RegistrarPagoFactura(IdFacturaHeader, pago);
            return NoContent();
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
