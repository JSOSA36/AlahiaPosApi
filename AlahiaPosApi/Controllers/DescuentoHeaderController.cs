using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DescuentoHeaderController : ControllerBase
    {
        private readonly IDescuentoHeader _service;

        public DescuentoHeaderController(IDescuentoHeader service)
        {
            _service = service;
        }

        // 🔹 Trae TODOS los descuentos de la empresa
        // GET: api/DescuentoHeader/Empresa/1
        [HttpGet("Empresa/{IdEmpresa}")]
        public async Task<IEnumerable<DescuentoHeaderDto>> GetAll(int IdEmpresa)
        {
            return await _service.GetAllDescuentoHeader(IdEmpresa);
        }

        // 🔹 Obtener descuento por Id
        // GET: api/DescuentoHeader/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DescuentoHeader>> GetById(int id)
        {
            var entity = await _service.GetDescuentoHeaderById(id);
            if (entity == null)
                return NotFound();

            return Ok(entity);
        }

        // 🔹 Obtener descuentos activos
        // GET: api/DescuentoHeader/Activos/1
        [HttpGet("Activos/{IdEmpresa}")]
        public async Task<IEnumerable<DescuentoHeader>> GetActivos(int IdEmpresa)
        {
            return await _service.GetDescuentosActivos(IdEmpresa);
        }

        // 🔹 Obtener descuentos activos por día
        // GET: api/DescuentoHeader/ActivosPorDia/1/2
        [HttpGet("ActivosPorDia/{IdEmpresa}/{diaSemana}")]
        public async Task<IEnumerable<DescuentoHeader>> GetActivosPorDia(int IdEmpresa, int diaSemana)
        {
            return await _service.GetDescuentosByDia(IdEmpresa, diaSemana);
        }

        // 🔹 Obtener descuentos por horario
        // GET: api/DescuentoHeader/Horario/1?horaActual=14:30
        [HttpGet("Horario/{IdEmpresa}")]
        public async Task<IEnumerable<DescuentoHeader>> GetPorHorario(int IdEmpresa, [FromQuery] string horaActual)
        {
            if (!TimeSpan.TryParse(horaActual, out var hora))
                return new List<DescuentoHeader>();

            return await _service.GetDescuentosPorHorario(IdEmpresa, hora);
        }

        // 🔹 Obtener descuentos por servicio + área
        // GET: api/DescuentoHeader/Servicio/1/123/5
        [HttpGet("Servicio/{IdEmpresa}/{IdProducto}/{IdArea}")]
        public async Task<IEnumerable<DescuentoHeader>> GetPorServicio(int IdEmpresa, int IdProducto, int IdArea)
        {
            return await _service.GetDescuentosPorServicio(IdEmpresa, IdProducto, IdArea);
        }

        // 🔹 Obtener descuentos VIGENTES AHORA (día + horario + servicio + área)
        // GET: api/DescuentoHeader/Vigentes/1/2/123/5?horaActual=14:30
        [HttpGet("Vigentes/{IdEmpresa}/{diaSemana}/{IdProducto}/{IdArea}")]
        public async Task<IEnumerable<DescuentoHeader>> GetVigentesAhora(
            int IdEmpresa,
            int diaSemana,
            int IdProducto,
            int IdArea,
            [FromQuery] string horaActual)
        {
            if (!TimeSpan.TryParse(horaActual, out var hora))
                return new List<DescuentoHeader>();

            return await _service.GetDescuentosVigentesAhora(
                IdEmpresa,
                diaSemana,
                hora,
                IdProducto,
                IdArea
            );
        }

        // 🔹 Crear descuento
        // POST: api/DescuentoHeader
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DescuentoHeaderDto dto)
        {
            if (dto == null)
                return BadRequest("El descuento no puede ser nulo.");

            try
            {
                await _service.InsertDescuentoHeader(dto);
                return Ok(new { message = "Descuento registrado correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        // 🔹 Actualizar descuento
        // PUT: api/DescuentoHeader/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DescuentoHeaderDto dto)
        {
            if (dto == null || id != dto.IdDescuentoHeader)
                return BadRequest("Datos inválidos.");

            _service.UpdateDescuentoHeader(id, dto);
            return NoContent();
        }
        [HttpGet("Aplicar")]
        public async Task<ActionResult<DescuentoAplicadoDto>> AplicarDescuento(
        int idEmpresa, int idProducto, int idArea)
        {
            var dto = await _service.GetDescuentoAplicado(idEmpresa, idProducto, idArea);
            return Ok(dto);
        }

        [HttpPatch("toggle/{id}")]
        public IActionResult Toggle(int id)
        {
            _service.ToggleEstado(id);
            return NoContent();
        }

        // 🔹 Eliminar descuento
        // DELETE: api/DescuentoHeader/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteDescuentoHeader(id);
            return NoContent();
        }
    }
}
