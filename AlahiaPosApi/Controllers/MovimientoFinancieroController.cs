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

        /* =====================================
        🔥 GET EMPRESA
        ===================================== */

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

        /* =====================================
        🔥 GET BY CUENTA
        ===================================== */

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

        /* =====================================
        🔥 GET BY FECHA
        ===================================== */

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

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        [HttpGet("GetById/{id}")]
        public async Task<MovimientoFinanciero?>
            GetById(
                int id
            )
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 ENTRADA
        ===================================== */

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

                dto.Observacion
            );

            return Ok();
        }
        /* =====================================
        🔥 SALIDA
        ===================================== */

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

                dto.Observacion
            );

            return Ok();
        }

        /* =====================================
        🔥 TRANSFERENCIA
        ===================================== */

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

        /* =====================================
        🔥 DELETE
        ===================================== */

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