using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/perfilesRoles")]
    public class PerfilesModulosController : ControllerBase
    {
        private readonly IPerfilRoles _perfilRoles;

        public PerfilesModulosController(IPerfilRoles perfilRoles)
        {
            _perfilRoles = perfilRoles;
        }

        // =====================================================
        // 🔹 OBTENER MÓDULOS DE UN PERFIL
        // =====================================================
        [HttpGet("{perfilId:int}/{idEmpresa:int}/modulos")]
        public async Task<IActionResult> GetModulos(int perfilId, int idEmpresa)
        {
            if (perfilId <= 0 || idEmpresa <= 0)
                return BadRequest("Parámetros inválidos");

            var modulos = await _perfilRoles.ObtenerModulos(perfilId, idEmpresa);

            var dtos = modulos.Select(m => new UsuarioModulo
            {
                ModuloId = m.Id,
                Codigo = m.Codigo,
                Nombre = m.Nombre
            });

            return Ok(dtos);
        }


        // =====================================================
        // 🔹 ASIGNAR MÓDULOS A PERFIL
        // =====================================================
        [HttpPost("{perfilId:int}/{idEmpresa:int}/modulos")]
        public async Task<IActionResult> AsignarModulos(
     int perfilId,
     int idEmpresa,
     [FromBody] AsignarPerfilModulosDto dto)
        {
            if (perfilId <= 0 || idEmpresa <= 0)
                return BadRequest("Parámetros inválidos");

            if (dto == null)
                return BadRequest("Body requerido");

            var modulosIds = dto.ModulosIds?.Distinct().ToList() ?? new List<int>();

            await _perfilRoles.AsignarModulos(perfilId, idEmpresa, modulosIds);

            return Ok(new
            {
                message = "Módulos asignados correctamente",
                total = modulosIds.Count
            });
        }
    }
}
