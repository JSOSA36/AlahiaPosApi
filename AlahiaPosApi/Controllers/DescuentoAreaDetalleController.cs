using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescuentoAreaDetalleController : ControllerBase
    {
        private readonly IDescuentoAreaDetalle _service;

        public DescuentoAreaDetalleController(IDescuentoAreaDetalle service)
        {
            _service = service;
        }

        // 🔹 Obtener todas las áreas asignadas a un descuento
        // GET: api/DescuentoAreaDetalle/Header/5
        [HttpGet("Header/{idHeader}")]
        public async Task<IEnumerable<DescuentoAreaDetalle>> GetByHeader(int idHeader)
        {
            return await _service.GetAreasByHeader(idHeader);
        }

        // 🔹 Obtener un detalle de área por ID
        // GET: api/DescuentoAreaDetalle/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DescuentoAreaDetalle>> Get(int id)
        {
            var entity = await _service.GetAreaDetalleById(id);
            if (entity == null)
                return NotFound("Detalle no encontrado");

            return Ok(entity);
        }

        // 🔹 Insertar una sola área
        // POST: api/DescuentoAreaDetalle
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DescuentoAreaDetalle dto)
        {
            if (dto == null)
                return BadRequest("El detalle no puede ser nulo");

            await _service.InsertArea(dto);
            return Ok(dto);
        }

        // 🔹 Insertar varias áreas en lote
        // POST: api/DescuentoAreaDetalle/Batch
        [HttpPost("Batch")]
        public async Task<IActionResult> PostBatch([FromBody] IEnumerable<DescuentoAreaDetalle> detalles)
        {
            if (detalles == null)
                return BadRequest("La lista no puede ser nula");

            await _service.InsertAreas(detalles);
            return Ok(detalles);
        }

        // 🔹 Actualizar un detalle
        // PUT: api/DescuentoAreaDetalle/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DescuentoAreaDetalle dto)
        {
            if (dto == null || id != dto.IdDescuentoAreaDetalle)
                return BadRequest("Datos inválidos");

            _service.UpdateArea(id, dto);
            return NoContent();
        }

        // 🔹 Eliminar un detalle por ID
        // DELETE: api/DescuentoAreaDetalle/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteArea(id);
            return NoContent();
        }

        // 🔹 Eliminar TODAS las áreas de un Header
        // DELETE: api/DescuentoAreaDetalle/Header/5
        [HttpDelete("Header/{idHeader}")]
        public async Task<IActionResult> DeleteByHeader(int idHeader)
        {
            await _service.DeleteAreasByHeader(idHeader);
            return NoContent();
        }
    }
}
