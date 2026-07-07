using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaMovimientoController
        : ControllerBase
    {

        ICajaMovimientoService _service;

        public CajaMovimientoController(
            ICajaMovimientoService service
        )
        {
            _service = service;
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        [HttpGet]
        public async Task<IEnumerable<CajaMovimiento>>
            Get()
        {
            return await _service
                .GetAllAsync();
        }

        /* =====================================
        🔥 GET BY CAJA
        ===================================== */

        [HttpGet]
        [Route("ByCaja/{idCaja}")]
        public async Task<IEnumerable<CajaMovimiento>>
            GetByCaja(int idCaja)
        {
            return await _service
                .GetByCajaAsync(idCaja);
        }

        /* =====================================
        🔥 ENTRADA
        ===================================== */

        [HttpPost]
        [Route("Entrada")]
        public async Task<IActionResult>
            Entrada(
                [FromBody]
                CajaMovimiento model
            )
        {

            await _service
                .RegistrarEntradaAsync(
                    model
                );

            return Ok(
                "Entrada registrada"
            );
        }

        /* =====================================
        🔥 SALIDA
        ===================================== */

        [HttpPost]
        [Route("Salida")]
        public async Task<IActionResult>
            Salida(
                [FromBody]
                CajaMovimiento model
            )
        {

            await _service
                .RegistrarSalidaAsync(
                    model
                );

            return Ok(
                "Salida registrada"
            );
        }

        /* =====================================
        🔥 DELETE
        ===================================== */

        [HttpDelete("{id}")]
        public async Task Delete(int id)
        {
            await _service
                .DeleteAsync(id);
        }
    }
}