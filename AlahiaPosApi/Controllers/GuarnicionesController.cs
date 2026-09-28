using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GuarnicionesController : ControllerBase
    {
        private readonly AlahiaPosContext _db;

        public GuarnicionesController(AlahiaPosContext db)
        {
            _db = db;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IActionResult> Get(int idEmpresa)
        {
            if (!TenantRecurso.EsDeLaSesion(HttpContext, idEmpresa))
                return NotFound();

            var lista = await _db.Guarniciones.AsNoTracking()
                .Where(g => g.IdEmpresa == idEmpresa)
                .OrderBy(g => g.Nombre)
                .ToListAsync();

            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Guarnicion value)
        {
            if (value == null || value.IdEmpresa <= 0)
                return BadRequest("Empresa requerida.");
            if (!TenantRecurso.EsDeLaSesion(HttpContext, value.IdEmpresa))
                return NotFound();

            var nombre = (value.Nombre ?? "").Trim();
            if (nombre.Length == 0)
                return BadRequest("El nombre es obligatorio.");

            var repetida = await _db.Guarniciones.AnyAsync(g =>
                g.IdEmpresa == value.IdEmpresa
                && g.Nombre.ToLower() == nombre.ToLower());
            if (repetida)
                return BadRequest("Esa guarnición ya está registrada.");

            var row = new Guarnicion
            {
                IdEmpresa = value.IdEmpresa,
                Nombre = nombre,
                Activo = true,
                FechaInseccion = DateTime.Now
            };
            _db.Guarniciones.Add(row);
            await _db.SaveChangesAsync();
            return Ok(row);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Guarnicion value)
        {
            var row = await _db.Guarniciones.FirstOrDefaultAsync(g => g.IdGuarnicion == id);
            if (row == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, row.IdEmpresa))
                return NotFound();

            var nombre = (value?.Nombre ?? "").Trim();
            if (nombre.Length == 0)
                return BadRequest("El nombre es obligatorio.");

            var repetida = await _db.Guarniciones.AnyAsync(g =>
                g.IdEmpresa == row.IdEmpresa
                && g.IdGuarnicion != id
                && g.Nombre.ToLower() == nombre.ToLower());
            if (repetida)
                return BadRequest("Esa guarnición ya está registrada.");

            row.Nombre = nombre;
            row.Activo = value?.Activo ?? row.Activo;
            await _db.SaveChangesAsync();
            return Ok(row);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var row = await _db.Guarniciones.FirstOrDefaultAsync(g => g.IdGuarnicion == id);
            if (row == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, row.IdEmpresa))
                return NotFound();

            _db.Guarniciones.Remove(row);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Guarnición eliminada.", id });
        }
    }
}
