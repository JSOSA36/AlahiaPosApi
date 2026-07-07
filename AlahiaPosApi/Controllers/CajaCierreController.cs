using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaCierreController
        : ControllerBase
    {

        ICajaCierreService _service;

        public CajaCierreController(
            ICajaCierreService service
        )
        {
            _service = service;
        }

        /* =====================================
        🔥 GET ALL
        ===================================== */

        [HttpGet]
        public async Task<IEnumerable<CajaListadoDto>>
            Get()
        {
            return await _service
                .GetAllAsync();
        }
        /* =====================================
🔥 GET BY FECHA
===================================== */

        [HttpGet]
        [Route("GetByFecha")]
        public async Task<IEnumerable<CajaListadoDto>>
GetByFecha(
    int idEmpresa,
    DateTime desde,
    DateTime hasta
)
        {
            return await _service.GetByFechaAsync(
                idEmpresa,
                desde,
                hasta
            );
        }
        /* =====================================
        🔥 GET BY ID
        ===================================== */

        [HttpGet("{id}")]
        public async Task<CajaCierre?>
            GetById(int id)
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 ÚLTIMO CIERRE
        ===================================== */

        [HttpGet]
        [Route("UltimoCierre")]
        public async Task<CajaCierre?>
            UltimoCierre(
                int idEmpresa
            )
        {
            return await _service
                .GetUltimoCierreAsync(
                    idEmpresa
                );
        }

        /* =====================================
        🔥 PROCESAR CIERRE
        ===================================== */

        [HttpPost]
        [Route("Procesar")]
        public async Task<IActionResult>
Procesar(

    [FromBody]
    CajaCierre model
)
        {

            if (model == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Modelo inválido"
                });
            }

            var cierre =

                await _service
                .ProcesarCierreAsync(
                    model
                );

            return Ok(new
            {
                success = true,
                message = "Caja cerrada correctamente",
                data = cierre
            });
        }


        [HttpGet]
        [Route("ImprimirCierre/{idCajaCierre}")]
        public async Task<IActionResult>
 ImprimirCierre(

     int idCajaCierre
 )
        {
            try
            {
                var cierre =

                    await _service
                    .ImprimirCierre(

                        idCajaCierre
                    );

                if (cierre == null)
                {
                    return NotFound(
                        new
                        {
                            success = false,
                            message = "Cierre no encontrado",
                            data = cierre
                        }
                    );
                }

                return Ok(cierre);
            }
            catch (Exception ex)
            {
                return BadRequest(
                    new
                    {
                        success = false,
                        message = ex.Message
                    }
                );
            }
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