using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalonController : ControllerBase
    {
        private readonly ISalonService _salon;

        public SalonController(ISalonService salon)
        {
            _salon = salon;
        }

        [HttpGet("snapshot")]
        public async Task<IActionResult> Snapshot(int idEmpresa, int? idZona = null)
        {
            if (idEmpresa <= 0)
            {
                return BadRequest(new { message = "Empresa inválida." });
            }

            return Ok(await _salon.ObtenerSnapshotAsync(idEmpresa, idZona));
        }

        [HttpGet("ordenes")]
        public async Task<IActionResult> OrdenesMesa(int idEmpresa, int idMesa)
        {
            if (idEmpresa <= 0 || idMesa <= 0)
            {
                return BadRequest(new { message = "Mesa inválida." });
            }

            return Ok(await _salon.ObtenerOrdenesMesaAsync(idEmpresa, idMesa));
        }

        [HttpPost("zonas")]
        public async Task<IActionResult> CrearZona([FromBody] SalonZonaWriteDto dto)
        {
            try
            {
                return Ok(await _salon.CrearZonaAsync(dto.IdEmpresa, dto.Nombre));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("zonas/{id}")]
        public async Task<IActionResult> RenombrarZona(int id, [FromBody] SalonZonaWriteDto dto)
        {
            try
            {
                return Ok(await _salon.RenombrarZonaAsync(dto.IdEmpresa, id, dto.Nombre));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("mesas")]
        public async Task<IActionResult> CrearMesa([FromBody] SalonMesaWriteDto dto)
        {
            try
            {
                return Ok(await _salon.CrearMesaAsync(dto.IdEmpresa, dto.ZonaId, dto.Numero, dto.Capacidad));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
