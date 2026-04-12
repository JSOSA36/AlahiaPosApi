using Microsoft.AspNetCore.Mvc;
using AlahiaPos.Entities.Interfaces;
using AlahiaPos.Entities.Dto;

namespace AlahiaPos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlanesCloudController : ControllerBase
    {
        private readonly IPlanesCloud _planesService;

        public PlanesCloudController(IPlanesCloud planesService)
        {
            _planesService = planesService;
        }

        // =====================================================
        // 📦 OBTENER TODOS LOS PLANES
        // =====================================================
        [HttpGet("GetPlanes")]
        public async Task<IActionResult> GetPlanes()
        {
            var planes = await _planesService.GetAllPlanes();
            return Ok(planes);
        }
        // 🔥 GET: api/planes/empresa/5
        [HttpGet("empresa/{idEmpresa}")]
        public async Task<IActionResult> GetPlanesPorEmpresa(int idEmpresa)
        {
            try
            {
                var planes = await _planesService.GetPlanesConActual(idEmpresa);

                return Ok(planes);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    mensaje = ex.Message
                });
            }
        }
    

        // =====================================================
        // 🔍 OBTENER PLAN POR ID
        // =====================================================
        [HttpGet("GetPlan/{id}")]
        public async Task<IActionResult> GetPlan(int id)
        {
            var plan = await _planesService.GetPlanById(id);

            if (plan == null)
                return NotFound("Plan no encontrado");

            return Ok(plan);
        }

        // =====================================================
        // 🔄 CAMBIAR PLAN
        // =====================================================
        [HttpPost("CambiarPlan")]
        public async Task<IActionResult> CambiarPlan([FromBody] CambiarPlanDto dto)
        {
            if (dto == null || dto.IdEmpresa <= 0 || dto.IdPlan <= 0)
                return BadRequest("Datos inválidos");

            try
            {
                await _planesService.CambiarPlan(dto.IdEmpresa, dto.IdPlan);

                return Ok(new
                {
                    mensaje = "Plan actualizado correctamente"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}