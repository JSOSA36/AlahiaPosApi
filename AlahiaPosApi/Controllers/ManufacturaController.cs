using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [ApiController]
    [Route("api/manufactura")]
    public class ManufacturaController : ControllerBase
    {
        private readonly IManufacturaService _svc;

        public ManufacturaController(IManufacturaService svc)
        {
            _svc = svc;
        }

        [HttpGet("recetas/{idEmpresa:int}")]
        public async Task<IActionResult> Recetas(int idEmpresa, [FromQuery] int? idProducto = null, [FromQuery] bool soloActivas = false) =>
            Ok(await _svc.ListarRecetasAsync(idEmpresa, idProducto, soloActivas));

        [HttpGet("recetas/{idEmpresa:int}/{idReceta:int}")]
        public async Task<IActionResult> Receta(int idEmpresa, int idReceta)
        {
            var dto = await _svc.ObtenerRecetaAsync(idEmpresa, idReceta);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPut("recetas")]
        public async Task<IActionResult> GuardarReceta([FromBody] GuardarRecetaRequest body)
        {
            try { return Ok(await _svc.GuardarRecetaAsync(body)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("recetas/{idEmpresa:int}/{idReceta:int}/explotar")]
        public async Task<IActionResult> Explotar(int idEmpresa, int idReceta, [FromQuery] decimal cantidad, [FromQuery] int? idAlmacenOrigen = null)
        {
            try { return Ok(await _svc.ExplotarAsync(idEmpresa, idReceta, cantidad, idAlmacenOrigen)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("ordenes/{idEmpresa:int}")]
        public async Task<IActionResult> Ordenes(int idEmpresa, [FromQuery] string? estado = null) =>
            Ok(await _svc.ListarOrdenesAsync(idEmpresa, estado));

        [HttpGet("ordenes/{idEmpresa:int}/{idOrden:int}")]
        public async Task<IActionResult> Orden(int idEmpresa, int idOrden)
        {
            var dto = await _svc.ObtenerOrdenAsync(idEmpresa, idOrden);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPut("ordenes")]
        public async Task<IActionResult> GuardarOrden([FromBody] GuardarOrdenProduccionRequest body)
        {
            try { return Ok(await _svc.GuardarOrdenAsync(body)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("ordenes/{idEmpresa:int}/{idOrden:int}/planificar")]
        public async Task<IActionResult> Planificar(int idEmpresa, int idOrden, [FromQuery] int idUsuario)
        {
            try { return Ok(await _svc.PlanificarAsync(idEmpresa, idOrden, idUsuario)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("ordenes/{idEmpresa:int}/{idOrden:int}/iniciar")]
        public async Task<IActionResult> Iniciar(int idEmpresa, int idOrden, [FromQuery] int idUsuario)
        {
            try { return Ok(await _svc.IniciarAsync(idEmpresa, idOrden, idUsuario)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("ordenes/{idOrden:int}/completar")]
        public async Task<IActionResult> Completar(int idOrden, [FromBody] CompletarOrdenProduccionRequest body)
        {
            try { return Ok(await _svc.CompletarAsync(idOrden, body)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("ordenes/{idEmpresa:int}/{idOrden:int}/cancelar")]
        public async Task<IActionResult> Cancelar(int idEmpresa, int idOrden, [FromQuery] int idUsuario)
        {
            try { return Ok(await _svc.CancelarAsync(idEmpresa, idOrden, idUsuario)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("ordenes/{idEmpresa:int}/{idOrden:int}/requerimiento-compra")]
        public async Task<IActionResult> Requerimiento(int idEmpresa, int idOrden, [FromQuery] int idUsuario)
        {
            try { return Ok(await _svc.CrearRequerimientoCompraAsync(idEmpresa, idOrden, idUsuario)); }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
            { return BadRequest(new { message = ex.Message }); }
        }
    }
}
