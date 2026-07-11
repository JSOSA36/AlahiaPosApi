using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenesController : ControllerBase
    {
        private readonly IAlmacenes _almacenes;

        public AlmacenesController(IAlmacenes almacenes)
        {
            _almacenes = almacenes;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<Almacen>> Get(int idEmpresa)
        {
            return await _almacenes.GetAllAlmacenes(idEmpresa);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var almacen = await _almacenes.GetAlmacenById(id);

            if (almacen == null)
            {
                return NotFound();
            }

            return Ok(almacen);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Almacen almacen)
        {
            if (almacen == null)
            {
                return BadRequest("El almacén no puede ser nulo.");
            }

            if (string.IsNullOrWhiteSpace(almacen.Nombre))
            {
                return BadRequest("El nombre es obligatorio.");
            }

            if (almacen.IdEmpresa <= 0)
            {
                return BadRequest("La empresa es obligatoria.");
            }

            almacen.Nombre = almacen.Nombre.Trim();
            almacen.Descripcion = almacen.Descripcion?.Trim();
            almacen.FechaCreacion = DateTime.Now;
            almacen.Activo = true;

            await _almacenes.InsertAlmacen(almacen);

            return Ok(almacen);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Almacen almacen)
        {
            if (almacen == null || id != almacen.IdAlmacen)
            {
                return BadRequest("Datos inválidos.");
            }

            if (string.IsNullOrWhiteSpace(almacen.Nombre))
            {
                return BadRequest("El nombre es obligatorio.");
            }

            var existente = await _almacenes.GetAlmacenById(id);

            if (existente == null)
            {
                return NotFound();
            }

            existente.Nombre = almacen.Nombre.Trim();
            existente.Descripcion = almacen.Descripcion?.Trim();
            existente.EsPrincipal = almacen.EsPrincipal;
            existente.Activo = almacen.Activo;
            existente.IdEmpresa = almacen.IdEmpresa;

            await _almacenes.UpdateAlmacen(id, existente);

            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _almacenes.DeleteAlmacen(id);
            return NoContent();
        }
    }
}
