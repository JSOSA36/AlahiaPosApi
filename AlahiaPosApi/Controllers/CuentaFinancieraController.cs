using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentaFinancieraController
        : ControllerBase
    {
        private readonly ICuentaFinancieraService
            _service;

        public CuentaFinancieraController(

            ICuentaFinancieraService
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
        public async Task<IEnumerable<CuentaFinanciera>>
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
        🔥 GET BY ID
        ===================================== */

        [HttpGet("GetById/{id}")]
        public async Task<CuentaFinanciera?>
            GetById(
                int id
            )
        {
            return await _service
                .GetByIdAsync(id);
        }

        /* =====================================
        🔥 GET BALANCE
        ===================================== */

        [HttpGet("Balance/{idCuentaFinanciera}")]
        public async Task<decimal>
            GetBalance(
                int idCuentaFinanciera
            )
        {
            return await _service
                .GetBalanceAsync(
                    idCuentaFinanciera
                );
        }

        /* =====================================
        🔥 CREATE
        ===================================== */

        [HttpPost]
        public async Task<IActionResult>
            Post(
                [FromBody]
                CuentaFinanciera entity
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
                CuentaFinanciera entity
            )
        {
            if (

                entity == null

                ||

                id !=
                entity.IdCuentaFinanciera
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