using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotasCreditoController : ControllerBase
    {
        private readonly INotasCredito _notasCredito;

        public NotasCreditoController(
            INotasCredito notasCredito)
        {
            _notasCredito = notasCredito;
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear(
            [FromBody] CrearNotaCreditoDto dto)
        {
            try
            {
                var resultado =
                    await _notasCredito.CrearNotaCredito(dto);

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("ticket/{idNotaCredito}/{idEmpresa}")]
        public async Task<IActionResult> GetTicket(
            int idNotaCredito,
            int idEmpresa)
        {
            var ticket =
                await _notasCredito.GetTicketNotaCredito(
                    idNotaCredito,
                    idEmpresa);

            if (ticket == null)
            {
                return NotFound(
                    "Nota de crédito no encontrada.");
            }

            return Ok(ticket);
        }
    }
}
