using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    /// <summary>
    /// Alta/gestión de empresas cliente — solo MacroBits (EsEmpresaSistema).
    /// Rutas bajo /api/EmpresaAdmin para no chocar con Empresa/{id}.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresaAdminController : ControllerBase
    {
        private readonly IEmpresaAdminService _service;
        private readonly IUsuarios _usuarios;
        private readonly IEmpresas _empresas;

        public EmpresaAdminController(
            IEmpresaAdminService service,
            IUsuarios usuarios,
            IEmpresas empresas)
        {
            _service = service;
            _usuarios = usuarios;
            _empresas = empresas;
        }

        [HttpGet("listado")]
        public async Task<IActionResult> Listado([FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            return Ok(await _service.ListarAsync());
        }

        [HttpGet("catalogo-modulos")]
        public async Task<IActionResult> CatalogoModulos(
            [FromQuery] int? idEmpresa = null,
            [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            return Ok(await _service.CatalogoModulosAsync(idEmpresa));
        }

        [HttpGet("verticales")]
        public async Task<IActionResult> Verticales([FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            return Ok(await _service.ListarVerticalesAsync());
        }

        [HttpGet("{idEmpresa:int}")]
        public async Task<IActionResult> Detalle(int idEmpresa, [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            var dto = await _service.ObtenerAsync(idEmpresa);
            if (dto == null) return NotFound(new { message = "Empresa no encontrada." });
            return Ok(dto);
        }

        [HttpPost("alta")]
        public async Task<IActionResult> Alta(
            [FromBody] EmpresaAdminAltaRequest req,
            [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            try
            {
                return Ok(await _service.AltaAsync(req));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpPut("{idEmpresa:int}/demo")]
        public async Task<IActionResult> Demo(
            int idEmpresa,
            [FromBody] EmpresaAdminDemoRequest req,
            [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            try
            {
                await _service.ActualizarDemoAsync(idEmpresa, req);
                return Ok(new { ok = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{idEmpresa:int}/nivel-soporte")]
        public async Task<IActionResult> NivelSoporte(
            int idEmpresa,
            [FromBody] EmpresaAdminNivelSoporteRequest req,
            [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            try
            {
                await _service.ActualizarNivelSoporteAsync(idEmpresa, req);
                return Ok(new { ok = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{idEmpresa:int}/modulos")]
        public async Task<IActionResult> Modulos(
            int idEmpresa,
            [FromBody] EmpresaAdminModulosRequest req,
            [FromHeader(Name = "X-IdUsuario")] int idUsuario = 0)
        {
            if (!await EsMacroBitsAsync(idUsuario))
                return StatusCode(403, new { message = "Solo MacroBits puede gestionar empresas." });
            try
            {
                await _service.SincronizarModulosAsync(idEmpresa, req);
                return Ok(new { ok = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private async Task<bool> EsMacroBitsAsync(int idUsuario)
        {
            if (idUsuario <= 0) return false;
            var user = await _usuarios.ObtenerPorId(idUsuario);
            if (user == null || user.IdEmpresa <= 0) return false;
            var emp = await _empresas.GetEmpresaById(user.IdEmpresa);
            return emp != null && emp.EsEmpresaSistema;
        }
    }
}
