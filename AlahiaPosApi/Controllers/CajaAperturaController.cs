using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaAperturaController : ControllerBase
    {

        ICajaAperturaService _service;

        public CajaAperturaController(
            ICajaAperturaService service
        )
        {
            _service = service;
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        [HttpGet]
        public async Task<IEnumerable<CajaApertura>>
  Get(int idEmpresa)
        {
            return await _service
                .GetAllAsync(idEmpresa);
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        [HttpGet("{id}")]
        public async Task<CajaApertura?>
            GetById(int id)
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 CAJA ABIERTA
        ===================================== */

        [HttpGet]
        [Route("CajaAbierta")]
        public async Task<CajaApertura?>
            CajaAbierta(
                int idEmpresa,
                int idUsuario
            )
        {
            return await _service
                .GetCajaAbiertaAsync(
                    idEmpresa,
                    idUsuario
                );
        }

        /* =====================================
        🔥 ABRIR
        ===================================== */

        [HttpPost]
        [Route("Abrir")]
        [RequiereTerminalPos]
        public async Task<IActionResult>
            AbrirCaja(
                [FromBody]
                CajaApertura model
            )
        {

            var existe =
                await _service
                .ExisteCajaAbiertaAsync(

                    model.IdEmpresa,
                    model.IdUsuario
                );

            if (existe)
            {
                return BadRequest(new
                {
                    success = false,

                    message =
                        "Ya existe una caja abierta"
                });
            }

            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null && sesion.IdSucursal > 0)
                model.IdSucursal = sesion.IdSucursal;

            await _service
                .AbrirCajaAsync(model);

            return Ok(new
            {
                success = true,

                message =
        "Caja abierta correctamente"
            });
        }

        /* =====================================
        🔥 CERRAR
        ===================================== */

        [HttpPut]
        [Route("Cerrar/{idCaja}")]
        [RequiereTerminalPos]
        public async Task<IActionResult>
            CerrarCaja(
                int idCaja
            )
        {

            await _service
                .CerrarCajaAsync(idCaja);

            return Ok(
                "Caja cerrada correctamente"
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