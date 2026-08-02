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

        /// <summary>
        /// Nota de crédito comercial (concepto/monto). No es devolución ni anulación.
        /// </summary>
        [HttpPost("CrearComercial")]
        public async Task<IActionResult> CrearComercial(
            [FromBody] CrearNotaCreditoComercialDto dto)
        {
            try
            {
                var resultado =
                    await _notasCredito.CrearNotaCreditoComercialAsync(dto);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Anular")]
        public async Task<IActionResult> Anular(
            [FromBody] AnularNotaCreditoDto dto)
        {
            if (dto == null)
                return BadRequest("Datos de anulación requeridos.");

            try
            {
                var motivo = (dto.Motivo ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(dto.UsuarioAnulo))
                    motivo = $"[{dto.UsuarioAnulo.Trim()}] {motivo}".Trim();

                var resultado =
                    await _notasCredito.AnularNotaCredito(
                        dto.IdNotaCredito,
                        dto.IdEmpresa,
                        dto.IdUsuario,
                        motivo);

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

        [HttpGet("listado/{idEmpresa}")]
        public async Task<IActionResult> Listado(
            int idEmpresa,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] bool soloConComprobante = false)
        {
            var lista =
                await _notasCredito.ListarNotasCredito(
                    idEmpresa,
                    desde,
                    hasta,
                    soloConComprobante);

            return Ok(lista);
        }

        [HttpPost("ReintentarEmision/{idNotaCredito}")]
        public async Task<IActionResult> ReintentarEmision(
            int idNotaCredito,
            [FromQuery] int idEmpresa,
            [FromQuery] int idUsuario)
        {
            try
            {
                var resultado = await _notasCredito.ReintentarEmisionAsync(
                    idNotaCredito,
                    idEmpresa,
                    idUsuario);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("saldos-a-favor/{idEmpresa}")]
        public async Task<IActionResult> ListarSaldosAFavor(
            int idEmpresa,
            [FromQuery] int? idCliente = null)
        {
            var lista = await _notasCredito.ListarSaldosAFavorAsync(
                idEmpresa,
                idCliente);
            return Ok(lista);
        }

        /// <summary>
        /// Busca saldo a favor por e-NCF o número interno de NC del cliente.
        /// </summary>
        [HttpGet("saldos-a-favor/{idEmpresa}/por-numero")]
        public async Task<IActionResult> ObtenerSaldoPorNumero(
            int idEmpresa,
            [FromQuery] int idCliente,
            [FromQuery] string numero)
        {
            try
            {
                var saldo = await _notasCredito.ObtenerSaldoAFavorPorNumeroAsync(
                    idEmpresa,
                    idCliente,
                    numero);
                if (saldo == null)
                    return NotFound("Saldo a favor no encontrado.");
                return Ok(saldo);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
