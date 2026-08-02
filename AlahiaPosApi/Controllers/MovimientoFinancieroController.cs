using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovimientoFinancieroController
        : ControllerBase
    {
        private readonly IMovimientoFinancieroService
            _service;

        public MovimientoFinancieroController(
            IMovimientoFinancieroService
                service
        )
        {
            _service =
                service;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<MovimientoFinanciero>>
            Get(
                int idEmpresa
            )
        {
            return await _service
                .GetByEmpresaAsync(
                    idEmpresa
                );
        }

        [HttpGet("ByCuenta/{idCuentaFinanciera}")]
        public async Task<IEnumerable<MovimientoFinanciero>>
            GetByCuenta(
                int idCuentaFinanciera
            )
        {
            return await _service
                .GetByCuentaAsync(
                    idCuentaFinanciera
                );
        }

        [HttpGet("ByFecha")]
        public async Task<IEnumerable<MovimientoFinanciero>>
            GetByFecha(
                int idEmpresa,
                DateTime desde,
                DateTime hasta
            )
        {
            return await _service
                .GetByFechaAsync(
                    idEmpresa,
                    desde,
                    hasta
                );
        }

        [HttpPost("Consultar")]
        public async Task<ActionResult<IEnumerable<MovimientoFinancieroListadoDto>>>
            Consultar([FromBody] MovimientoFinancieroFiltroDto filtro)
        {
            return Ok(await _service.ConsultarAsync(filtro));
        }

        [HttpGet("EstadoCuenta/{idCuentaFinanciera}")]
        public async Task<ActionResult<EstadoCuentaDto>>
            EstadoCuenta(
                int idCuentaFinanciera,
                [FromQuery] DateTime? desde = null,
                [FromQuery] DateTime? hasta = null)
        {
            return Ok(await _service.GetEstadoCuentaAsync(idCuentaFinanciera, desde, hasta));
        }

        [HttpGet("GetById/{id}")]
        public async Task<MovimientoFinanciero?>
            GetById(
                int id
            )
        {
            return await _service
                .GetByIdAsync(id);
        }

        [HttpPost("Entrada")]
        public async Task<IActionResult>
            RegistrarEntrada(
                [FromBody]
                EntradaFinancieraDto dto
            )
        {
            await _service
            .RegistrarEntradaAsync(
                dto.IdEmpresa,
                dto.IdUsuario,
                dto.IdCuentaDestino,
                dto.Monto,
                dto.Motivo,
                dto.Observacion,
                dto.Categoria,
                dto.ReferenciaId,
                dto.ReferenciaTipo,
                dto.ClaveIdempotencia
            );

            return Ok();
        }

        [HttpPost("Salida")]
        public async Task<IActionResult>
            RegistrarSalida(
                [FromBody]
                SalidaFinancieraDto dto
            )
        {
            await _service
            .RegistrarSalidaAsync(
                dto.IdEmpresa,
                dto.IdUsuario,
                dto.IdCuentaOrigen,
                dto.Monto,
                dto.Motivo,
                dto.Observacion,
                dto.Categoria,
                dto.ReferenciaId,
                dto.ReferenciaTipo,
                dto.ClaveIdempotencia
            );

            return Ok();
        }

        [HttpPost("Transferencia")]
        public async Task<IActionResult>
            RegistrarTransferencia(
                [FromBody]
                TransferenciaFinancieraDto dto
            )
        {
            await _service
            .RegistrarTransferenciaAsync(
                dto.IdEmpresa,
                dto.IdUsuario,
                dto.IdCuentaOrigen,
                dto.IdCuentaDestino,
                dto.Monto,
                dto.Motivo,
                dto.Observacion
            );

            return Ok();
        }

        [HttpPost("Ajuste")]
        public async Task<ActionResult<int>> RegistrarAjuste([FromBody] RegistrarAjusteDto dto)
        {
            var id = await _service.RegistrarAjusteAsync(dto);
            return Ok(id);
        }

        [HttpPost("Anular")]
        public async Task<IActionResult> Anular([FromBody] AnularMovimientoDto dto)
        {
            await _service.AnularMovimientoAsync(dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult>
            Delete(
                int id
            )
        {
            return BadRequest(new
            {
                message = "Los movimientos de tesorería no se eliminan. Use un movimiento de ajuste o anulación."
            });
        }
    }
}
