using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TesoreriaConciliacionController : ControllerBase
    {
        private readonly ITesoreriaConciliacionService _service;

        public TesoreriaConciliacionController(ITesoreriaConciliacionService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<ActionResult<TesoreriaConciliacion>> Crear([FromBody] CrearConciliacionDto dto)
        {
            try
            {
                return Ok(await _service.CrearConciliacionAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{idEmpresa}/{idConciliacion}")]
        public async Task<ActionResult<TesoreriaConciliacion>> Get(int idEmpresa, int idConciliacion)
        {
            var result = await _service.GetByIdAsync(idConciliacion, idEmpresa);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("{idEmpresa}/{idConciliacion}/Workspace")]
        public async Task<ActionResult<ConciliacionWorkspaceDto>> Workspace(int idEmpresa, int idConciliacion)
        {
            try
            {
                return Ok(await _service.GetWorkspaceAsync(idConciliacion, idEmpresa));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("AdjuntarExtracto")]
        public async Task<ActionResult<TesoreriaExtractoImport>> AdjuntarExtracto(
            [FromBody] AdjuntarExtractoAConciliacionDto dto)
        {
            try
            {
                return Ok(await _service.AdjuntarExtractoAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idConciliacion}/ImportarExtracto")]
        public async Task<ActionResult<TesoreriaExtractoImport>> ImportarExtracto(
            int idEmpresa,
            int idConciliacion,
            [FromBody] ImportarExtractoDto dto)
        {
            try
            {
                dto.IdEmpresa = idEmpresa;
                return Ok(await _service.ImportarYAdjuntarAsync(idConciliacion, dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Matching")]
        public async Task<ActionResult<ConciliacionWorkspaceDto>> Matching(
            [FromBody] EjecutarMatchingConciliacionDto dto)
        {
            try
            {
                return Ok(await _service.EjecutarMatchingAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ResolverLinea")]
        public async Task<ActionResult<ResolverExtractoLineaResultadoDto>> ResolverLinea(
            [FromBody] ResolverLineaConciliacionDto dto)
        {
            try
            {
                return Ok(await _service.ResolverLineaAsync(dto));
            }
            // Los servicios de movimientos lanzan System.Exception para reglas de
            // negocio (p. ej. "Fondos insuficientes"); devolver el mensaje real
            // en vez de un 500 opaco.
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("DeshacerMatch")]
        public async Task<IActionResult> DeshacerMatch([FromBody] DeshacerMatchConciliacionDto dto)
        {
            try
            {
                await _service.DeshacerMatchAsync(dto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ReversarReclasificacion")]
        public async Task<ActionResult<ReclasificarPagoResultadoDto>> ReversarReclasificacion(
            [FromBody] ReversarReclasificacionPagoDto dto)
        {
            try
            {
                return Ok(await _service.ReversarReclasificacionAsync(dto));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("BuscarCandidatos")]
        public async Task<ActionResult<IEnumerable<MovimientoFinancieroListadoDto>>> BuscarCandidatos(
            [FromBody] BuscarCandidatosMatchDto dto)
        {
            try
            {
                return Ok(await _service.BuscarCandidatosAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{idEmpresa}/{idConciliacion}/Pendientes")]
        public async Task<ActionResult<IEnumerable<MovimientoFinancieroListadoDto>>> Pendientes(
            int idEmpresa,
            int idConciliacion)
        {
            return Ok(await _service.ListarMovimientosPendientesAsync(idConciliacion, idEmpresa));
        }

        [HttpPost("Marcar")]
        public async Task<IActionResult> Marcar([FromBody] MarcarConciliacionMovimientosDto dto)
        {
            try
            {
                await _service.MarcarConciliadosAsync(dto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Desmarcar")]
        public async Task<IActionResult> Desmarcar([FromBody] MarcarConciliacionMovimientosDto dto)
        {
            try
            {
                await _service.DesmarcarConciliadosAsync(dto);
                return Ok();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("CargoInteres")]
        public async Task<ActionResult<int>> CargoInteres([FromBody] RegistrarCargoInteresDto dto)
        {
            try
            {
                return Ok(await _service.RegistrarCargoInteresAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idEmpresa}/{idConciliacion}/Cerrar")]
        public async Task<ActionResult<TesoreriaConciliacion>> Cerrar(
            int idEmpresa,
            int idConciliacion,
            [FromQuery] int idUsuario)
        {
            try
            {
                return Ok(await _service.CerrarConciliacionAsync(idConciliacion, idEmpresa, idUsuario));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Reabrir")]
        public async Task<ActionResult<TesoreriaConciliacion>> Reabrir([FromBody] ReabrirConciliacionDto dto)
        {
            try
            {
                return Ok(await _service.ReabrirConciliacionAsync(dto));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Historial/{idEmpresa}")]
        public async Task<ActionResult<IEnumerable<ConciliacionResumenDto>>> Historial(
            int idEmpresa,
            [FromQuery] int? idCuentaFinanciera = null)
        {
            return Ok(await _service.GetHistorialAsync(idEmpresa, idCuentaFinanciera));
        }

        [HttpGet("PendientesConciliacion/{idEmpresa}/{idCuentaFinanciera}")]
        public async Task<ActionResult<IEnumerable<MovimientoFinancieroListadoDto>>> PendientesConciliacion(
            int idEmpresa,
            int idCuentaFinanciera,
            [FromQuery] DateTime? hasta = null)
        {
            return Ok(await _service.GetPendientesConciliacionAsync(idEmpresa, idCuentaFinanciera, hasta));
        }
    }
}
