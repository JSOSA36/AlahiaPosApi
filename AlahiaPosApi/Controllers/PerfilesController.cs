using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AlahiaPos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerfilesController : ControllerBase
    {
        private readonly IPerfiles _perfilesService;

        public PerfilesController(IPerfiles perfilesService)
        {
            _perfilesService = perfilesService;
        }

        // =====================================================
        // 📋 OBTENER PERFILES POR EMPRESA
        // =====================================================
        [HttpGet("empresa/{idEmpresa}")]
        public async Task<IActionResult> ObtenerPorEmpresa(int idEmpresa)
        {
            var perfiles = await _perfilesService.ObtenerPorEmpresa(idEmpresa);
            return Ok(perfiles);
        }

        // =====================================================
        // 🔎 OBTENER PERFIL POR ID
        // =====================================================
        [HttpGet("{idPerfil}")]
        public async Task<IActionResult> ObtenerPorId(int idPerfil)
        {
            var perfil = await _perfilesService.ObtenerPorId(idPerfil);

            if (perfil == null)
                return NotFound();

            return Ok(perfil);
        }

        // =====================================================
        // 🔥 CREAR PERFIL COMPLETO (PERFIL + ROLES)
        // =====================================================
        [HttpPost("completo")]
        public async Task<IActionResult> CrearPerfilCompleto(
            [FromBody] PerfilCreateDto dto
        )
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var idPerfil = await _perfilesService.CrearPerfilCompleto(dto);

            return Ok(new { idPerfil });
        }

        // =====================================================
        // ✏️ ACTUALIZAR PERFIL (SOLO DATOS BÁSICOS)
        // =====================================================
        [HttpPut("{idPerfil}")]
        public async Task<IActionResult> Actualizar(
            int idPerfil,
            [FromBody] Perfiles perfil
        )
        {
            if (idPerfil != perfil.IdPerfil)
                return BadRequest("El Id del perfil no coincide");

            var actualizado = await _perfilesService.Actualizar(perfil);

            if (!actualizado)
                return NotFound();

            return NoContent();
        }

        // =====================================================
        // 🗑️ ELIMINAR PERFIL (SOFT DELETE)
        // =====================================================
        [HttpDelete("{idPerfil}")]
        public async Task<IActionResult> Eliminar(int idPerfil)
        {
            var eliminado = await _perfilesService.Eliminar(idPerfil);

            if (!eliminado)
                return NotFound();

            return NoContent();
        }
        [HttpPut("completo/{idPerfil}")]
        public async Task<IActionResult> ActualizarPerfilCompleto(
    int idPerfil,
    [FromBody] PerfilUpdateDto dto
)
        {
            if (idPerfil != dto.IdPerfil)
                return BadRequest("IdPerfil no coincide");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ok = await _perfilesService.ActualizarPerfilCompleto(dto);

            if (!ok)
                return NotFound();

            return NoContent();
        }

    }
}
