using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetodoPagoCuentaController
        : ControllerBase
    {
        private readonly IMetodoPagoCuentaService
            _service;

        public MetodoPagoCuentaController(

            IMetodoPagoCuentaService
                service
        )
        {
            _service =
                service;
        }

        /* =====================================
        🔥 GET ALL EMPRESA
        ===================================== */

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<MetodoPagoCuenta>>
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
        🔥 GET BY METODO
        ===================================== */

        [HttpGet("ByMetodo")]
        public async Task<MetodoPagoCuenta?>
            GetByMetodo(

                int idEmpresa,

                string metodoPago
            )
        {
            return await _service
                .GetByMetodoAsync(

                    idEmpresa,

                    metodoPago
                );
        }

        /* =====================================
        🔥 GET BY ID
        ===================================== */

        [HttpGet("GetById/{id}")]
        public async Task<MetodoPagoCuenta?>
            GetById(
                int id
            )
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        [HttpPost]
        public async Task<IActionResult>
            Post(
                [FromBody]
                MetodoPagoCuenta entity
            )
        {
            if (entity == null)
                return BadRequest();

            await _service
                .CreateAsync(entity);

            return Ok(entity);
        }

        /* =====================================
        🔥 UPDATE
        ===================================== */

        [HttpPut("{id}")]
        public async Task<IActionResult>
            Put(

                int id,

                [FromBody]
                MetodoPagoCuenta entity
            )
        {
            if (

                entity == null

                ||

                id !=
                entity.IdMetodoPagoCuenta
            )
            {
                return BadRequest();
            }

            await _service
                .UpdateAsync(entity);

            return NoContent();
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
            await _service
                .DeleteAsync(id);

            return NoContent();
        }
    }
}