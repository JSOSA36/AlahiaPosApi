using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/produccion")]
    [ApiController]
    public class ProduccionController : ControllerBase
    {
        private readonly IProduccionTrabajoService _trabajos;
        private readonly IProduccionConfiguracionService _config;
        private readonly IProduccionFlujoService _flujos;
        private readonly IProduccionRealtime _realtime;

        public ProduccionController(
            IProduccionTrabajoService trabajos,
            IProduccionConfiguracionService config,
            IProduccionFlujoService flujos,
            IProduccionRealtime realtime)
        {
            _trabajos = trabajos;
            _config = config;
            _flujos = flujos;
            _realtime = realtime;
        }

        /// <summary>Hydrate: trabajos activos al abrir o reconectar el tablero.</summary>
        [HttpGet("trabajos/activos")]
        public async Task<IActionResult> ListarActivos([FromQuery] int idEmpresa, [FromQuery] string? tipoTrabajo = null)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _trabajos.ListarActivosAsync(idEmpresa, tipoTrabajo);
            return Ok(data);
        }

        /// <summary>Estados por documento origen (listado de órdenes). Incluye fuera del tablero.</summary>
        [HttpGet("trabajos/estados-por-origen")]
        public async Task<IActionResult> EstadosPorOrigen(
            [FromQuery] int idEmpresa,
            [FromQuery] string origenTipo = ProduccionConstantes.OrigenTipoFacturaHeader,
            [FromQuery] string? origenIds = null)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var ids = (origenIds ?? "")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();

            var data = await _trabajos.ObtenerEstadosPorOrigenAsync(idEmpresa, origenTipo, ids);
            return Ok(data);
        }

        [HttpGet("trabajos/{idTrabajo:int}")]
        public async Task<IActionResult> Obtener(int idTrabajo, [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _trabajos.ObtenerAsync(idEmpresa, idTrabajo);
            if (data == null)
                return NotFound(new { message = "Trabajo no encontrado." });
            return Ok(data);
        }

        [HttpPost("trabajos/{idTrabajo:int}/transicion")]
        public async Task<IActionResult> Transicionar(int idTrabajo, [FromQuery] int idEmpresa, [FromBody] ProduccionTransicionRequest request)
        {
            try
            {
                var data = await _trabajos.TransicionarAsync(idEmpresa, idTrabajo, request ?? new ProduccionTransicionRequest());
                await _realtime.EmitirTrabajoEstadoAsync(data);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return MapBusinessError(ex);
            }
        }

        [HttpPost("trabajos/{idTrabajo:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int idTrabajo, [FromQuery] int idEmpresa, [FromBody] ProduccionCancelarRequest request)
        {
            try
            {
                var data = await _trabajos.CancelarAsync(idEmpresa, idTrabajo, request ?? new ProduccionCancelarRequest());
                await _realtime.EmitirTrabajoEstadoAsync(data);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return MapBusinessError(ex);
            }
        }

        [HttpPost("trabajos/{idTrabajo:int}/prioridad")]
        public async Task<IActionResult> Prioridad(int idTrabajo, [FromQuery] int idEmpresa, [FromBody] ProduccionPrioridadRequest request)
        {
            try
            {
                var data = await _trabajos.CambiarPrioridadAsync(idEmpresa, idTrabajo, request ?? new ProduccionPrioridadRequest());
                await _realtime.EmitirTrabajoEstadoAsync(data);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return MapBusinessError(ex);
            }
        }

        [HttpGet("trabajos/{idTrabajo:int}/historial")]
        public async Task<IActionResult> Historial(int idTrabajo, [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _trabajos.ListarHistorialAsync(idEmpresa, idTrabajo);
            return Ok(data);
        }

        [HttpGet("dashboard/resumen")]
        public async Task<IActionResult> Dashboard([FromQuery] int idEmpresa, [FromQuery] string? tipoTrabajo = null)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _trabajos.ObtenerDashboardAsync(idEmpresa, tipoTrabajo);
            return Ok(data);
        }

        [HttpGet("config")]
        public async Task<IActionResult> ObtenerConfig([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _config.ObtenerAsync(idEmpresa);
            if (data == null)
                return NotFound(new { message = "Configuración no encontrada." });
            return Ok(data);
        }

        [HttpPut("config")]
        public async Task<IActionResult> ActualizarConfig([FromBody] ProduccionConfiguracionDto dto)
        {
            try
            {
                var data = await _config.ActualizarAsync(dto);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("flujos")]
        public async Task<IActionResult> ListarFlujos([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _flujos.ListarFlujosAsync(idEmpresa);
            return Ok(data);
        }

        [HttpGet("flujos/activo")]
        public async Task<IActionResult> FlujoActivo([FromQuery] int idEmpresa, [FromQuery] string tipoTrabajo = ProduccionConstantes.TipoPosOrden)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });

            var data = await _flujos.ObtenerFlujoActivoAsync(idEmpresa, tipoTrabajo);
            if (data == null)
                return NotFound(new { message = "No hay flujo activo para el tipo de trabajo." });
            return Ok(data);
        }

        private IActionResult MapBusinessError(InvalidOperationException ex)
        {
            var msg = ex.Message ?? "";
            if (msg.Contains("modificado por otro usuario", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("no coincide con el actual", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = msg });
            }

            if (msg.Contains("no encontrado", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { message = msg });

            return BadRequest(new { message = msg });
        }
    }
}
