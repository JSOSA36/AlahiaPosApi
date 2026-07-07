using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LavadorConsumoController : ControllerBase
    {
        private readonly ILavadorConsumoServices _lavadorConsumo;

        public LavadorConsumoController(ILavadorConsumoServices lavadorConsumo)
        {
            _lavadorConsumo = lavadorConsumo;
        }

        // 🔥 Registrar consumo (fiado)
        [HttpPost("Registrar")]
        public async Task<IActionResult> RegistrarConsumo([FromBody] LavadorConsumoCreateDto dto)
        {
            if (dto == null || dto.Monto <= 0)
                return BadRequest("Datos inválidos");

            
            await _lavadorConsumo.RegistrarConsumo(dto);

            return Ok(new { message = "Consumo registrado correctamente" });
        }
        // 🔥 DASHBOARD DEL LAVADOR
        [HttpGet]
        [Route("DashboardLavador")]
        public async Task<IActionResult> DashboardLavador(
    int idEmpleado,
    int idEmpresa,
    DateTime desde,
    DateTime hasta)
        {
            var result = await _lavadorConsumo.GetDashboardLavador(
                idEmpleado,
                idEmpresa,
                desde,
                hasta
            );

            if (result == null)
                return NotFound();

            return Ok(result);
        }
        // 🔥 Abonar consumo parcial
        [HttpPost("Abonar")]
        public async Task<IActionResult> AbonarConsumo([FromQuery] int idConsumo, [FromQuery] decimal monto)
        {
            if (monto <= 0)
                return BadRequest("Monto inválido");

            await _lavadorConsumo.AbonarConsumo(idConsumo, monto);

            return Ok("Abono aplicado correctamente");
        }

        // 🔥 Balance total del lavador
        [HttpGet("Balance")]
        public async Task<IActionResult> BalanceLavador(
            [FromQuery] int idEmpleado,
            [FromQuery] int idEmpresa)
        {
            var balance = await _lavadorConsumo.GetBalanceLavador(idEmpleado, idEmpresa);

            return Ok(balance);
        }

        // 🔥 Consumos pendientes del lavador
        [HttpGet("Pendientes")]
        public async Task<IActionResult> PendientesLavador(
            [FromQuery] int idEmpleado,
            [FromQuery] int idEmpresa)
        {
            var data = await _lavadorConsumo.GetPendientesByEmpleado(idEmpleado, idEmpresa);

            return Ok(data);
        }

        // 🔥 Historial por fechas ⭐ NUEVO ENDPOINT IMPORTANTE
        [HttpGet("Historial")]
        public async Task<IActionResult> HistorialLavador(
            [FromQuery] int idEmpleado,
            [FromQuery] int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            var data = await _lavadorConsumo.GetHistorialByEmpleado(
                idEmpleado,
                idEmpresa,
                desde,
                hasta);

            return Ok(data);
        }

        // 🔥 Listado administrativo general
        [HttpGet("Listado")]
        public async Task<IActionResult> ListadoGeneral([FromQuery] int idEmpresa)
        {
            var data = await _lavadorConsumo.GetListadoGeneral(idEmpresa);

            return Ok(data);
        }

        // 🔥 Saldar consumo completo
        [HttpPost("Saldar")]
        public async Task<IActionResult> SaldarConsumo([FromQuery] int idConsumo)
        {
            await _lavadorConsumo.SaldarConsumo(idConsumo);

            return Ok("Consumo saldado correctamente");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _lavadorConsumo.EliminarConsumo(id);
            return Ok();
        }
    }
}