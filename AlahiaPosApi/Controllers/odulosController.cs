using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModulosController : ControllerBase
    {
        private readonly IModulo _IModulos;
        private readonly IMapper _Mapper;

        public ModulosController(IModulo iModulos, IMapper mapper)
        {
            _IModulos = iModulos;
            _Mapper = mapper;
        }

        // 🔹 GET: api/Modulos
        [HttpGet]
        public async Task<IEnumerable<Modulo>> Get()
        {
            return await _IModulos.GetAllModulos();
        }

        // 🔹 GET: api/Modulos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Modulo>> GetById(int id)
        {
            var modulo = await _IModulos.GetModuloById(id);
            if (modulo == null)
                return NotFound();

            return Ok(modulo);
        }

        // 🔹 GET: api/Modulos/GetByCodigo/CITAS
        [HttpGet("GetByCodigo/{codigo}")]
        public async Task<ActionResult<Modulo>> GetByCodigo(int codigo)
        {
            var modulo = await _IModulos.GetModuloById(codigo);
            if (modulo == null)
                return NotFound();

            return Ok(modulo);
        }

        // 🔹 POST: api/Modulos
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] ModuloDto value)
        {
            try
            {
                var modulo = _Mapper.Map<Modulo>(value);
                await _IModulos.InsertModulo(modulo);

                return Ok(new { message = "✅ Módulo creado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al crear módulo", error = ex.Message });
            }
        }

        // 🔹 PUT: api/Modulos
        [HttpPut]
        public IActionResult Put([FromForm] ModuloDto value)
        {
            var modulo = _Mapper.Map<Modulo>(value);
            _IModulos.UpdateModulo(modulo.Id, modulo);

            return Ok(new { message = "✅ Módulo actualizado correctamente" });
        }

        // 🔹 DELETE: api/Modulos/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _IModulos.DeleteModulo(id);
            return Ok(new { message = "✅ Módulo eliminado correctamente" });
        }
    }
}
