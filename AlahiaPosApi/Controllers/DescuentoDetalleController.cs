using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescuentoDetalleController : ControllerBase
    {
        private readonly IDescuentoDetalle _service;

        public DescuentoDetalleController(IDescuentoDetalle service)
        {
            _service = service;
        }

        // 🔹 Obtener todos los detalles de un Header
        // GET: api/DescuentoDetalle/Header/5
        [HttpGet("Header/{idHeader}")]
        public async Task<IEnumerable<DescuentoDetalle>> GetByHeader(int idHeader)
        {
            return await _service.GetDetallesByHeader(idHeader);
        }

        // 🔹 Obtener un detalle por Id
        // GET: api/DescuentoDetalle/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DescuentoDetalle>> Get(int id)
        {
            var entity = await _service.GetDetalleById(id);
            if (entity == null)
                return NotFound("Detalle no encontrado");

            return Ok(entity);
        }

        // 🔹 Crear un detalle individual
        // POST: api/DescuentoDetalle
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DescuentoDetalle dto)
        {
            if (dto == null)
                return BadRequest("El detalle no puede ser nulo");

            await _service.InsertDetalle(dto);
            return Ok(dto);
        }

        // 🔹 Crear varios detalles en lote
        // POST: api/DescuentoDetalle/Batch
        [HttpPost("Batch")]
        public async Task<IActionResult> PostBatch([FromBody] IEnumerable<DescuentoDetalle> detalles)
        {
            if (detalles == null)
                return BadRequest("La lista no puede ser nula");

            await _service.InsertDetalles(detalles);
            return Ok(detalles);
        }

        // 🔹 Actualizar un detalle
        // PUT: api/DescuentoDetalle/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DescuentoDetalle dto)
        {
            if (dto == null || id != dto.IdDescuentoDetalle)
                return BadRequest("Datos inválidos");

            _service.UpdateDetalle(id, dto);
            return NoContent();
        }

        // 🔹 Eliminar un detalle
        // DELETE: api/DescuentoDetalle/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteDetalle(id);
            return NoContent();
        }

        // 🔹 Eliminar TODOS los detalles de un Header
        // DELETE: api/DescuentoDetalle/Header/5
        [HttpDelete("Header/{idHeader}")]
        public async Task<IActionResult> DeleteByHeader(int idHeader)
        {
            await _service.DeleteDetallesByHeader(idHeader);
            return NoContent();
        }
    }
}
