using System;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/pedidos-delivery")]
    [ApiController]
    public class PedidosDeliveryController : ControllerBase
    {
        private readonly IPedidosOnlineService _svc;

        public PedidosDeliveryController(IPedidosOnlineService svc)
        {
            _svc = svc;
        }

        [HttpGet("canal")]
        public async Task<IActionResult> Canal([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });
            var canal = await _svc.ObtenerCanalEmpresaAsync(idEmpresa);
            if (canal == null)
                return NotFound(new { message = "Esta empresa no tiene canal de pedidos en línea." });
            return Ok(canal);
        }

        [HttpGet("cola")]
        public async Task<IActionResult> Cola([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });
            return Ok(await _svc.ListarColaDeliveryAsync(idEmpresa));
        }

        [HttpGet]
        public async Task<IActionResult> Listar([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });
            return Ok(await _svc.ListarTodosAsync(idEmpresa));
        }

        [HttpGet("mios")]
        public async Task<IActionResult> Mios([FromQuery] int idEmpresa, [FromQuery] int idUsuario)
        {
            if (idEmpresa <= 0 || idUsuario <= 0)
                return BadRequest(new { message = "idEmpresa e idUsuario son obligatorios." });
            return Ok(await _svc.ListarMisPedidosAsync(idEmpresa, idUsuario));
        }

        [HttpGet("{idPedidoOnline:int}")]
        public async Task<IActionResult> Obtener(int idPedidoOnline, [FromQuery] int idEmpresa)
        {
            var data = await _svc.ObtenerPedidoAsync(idEmpresa, idPedidoOnline);
            if (data == null)
                return NotFound(new { message = "Pedido no encontrado." });
            return Ok(data);
        }

        [HttpGet("repartidores")]
        public async Task<IActionResult> Repartidores([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio." });
            return Ok(await _svc.ListarRepartidoresAsync(idEmpresa));
        }

        [HttpPost("repartidores")]
        public async Task<IActionResult> UpsertRepartidor([FromQuery] int idEmpresa, [FromBody] DeliveryRepartidorUpsertRequest request)
        {
            try
            {
                return Ok(await _svc.UpsertRepartidorAsync(idEmpresa, request ?? new DeliveryRepartidorUpsertRequest()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("asignar")]
        public async Task<IActionResult> Asignar([FromQuery] int idEmpresa, [FromBody] DeliveryAsignarRequest request)
        {
            try
            {
                return Ok(await _svc.AsignarAsync(idEmpresa, request ?? new DeliveryAsignarRequest()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{idPedidoOnline:int}/estado")]
        public async Task<IActionResult> Estado(int idPedidoOnline, [FromQuery] int idEmpresa, [FromBody] DeliveryTransicionRequest request)
        {
            try
            {
                return Ok(await _svc.TransicionarDeliveryAsync(idEmpresa, idPedidoOnline, request ?? new DeliveryTransicionRequest()));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
