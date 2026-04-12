using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpleadoAreaComisionController : ControllerBase
    {
        private readonly IEmpleadoAreaComisionService _empleadoAreaComisionService;
        private readonly IMapper _mapper;
        private readonly IAreas _IAreas;
        public EmpleadoAreaComisionController(
            IEmpleadoAreaComisionService empleadoAreaComisionService,
            IMapper mapper, IAreas areas)
        {
            _empleadoAreaComisionService = empleadoAreaComisionService;
            _mapper = mapper;
            _IAreas = areas;
        }

        // ✅ GET: api/EmpleadoAreaComision/{idEmpleado}
        
        [HttpGet("GetByEmpleado/{idEmpleado}")]
        public async Task<ActionResult<IEnumerable<EmpleadoAreaComisionDto>>> GetByEmpleado(int idEmpleado)
        {
            var comisiones = await _empleadoAreaComisionService.GetAllComisionesByEmpleado(idEmpleado);
            List<EmpleadoAreaComision> ListadoComision = new List<EmpleadoAreaComision>();
            foreach (var comision in comisiones) {

                var _Area = await _IAreas.GetAreaById((int)comision.IdArea);

                comision.Area = _Area;

                ListadoComision.Add(comision);
            }

           
            if (comisiones == null || !comisiones.Any())
                return NotFound(new { message = "No hay comisiones registradas para este empleado." });

            return Ok(_mapper.Map<IEnumerable<EmpleadoAreaComisionDto>>(ListadoComision));
        }

        // ✅ GET: api/EmpleadoAreaComision/GetById/{id}
        [HttpGet("GetById/{id}")]
        public async Task<ActionResult<EmpleadoAreaComisionDto>> GetById(int id)
        {
            var comision = await _empleadoAreaComisionService.GetComisionById(id);
            if (comision == null)
                return NotFound(new { message = "Comisión no encontrada" });

            return Ok(_mapper.Map<EmpleadoAreaComisionDto>(comision));
        }

        // ✅ POST: api/EmpleadoAreaComision
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] List<EmpleadoAreaComisionDto> comisiones)
        {
            if (comisiones == null || !comisiones.Any())
                return BadRequest(new { message = "Debe enviar al menos una comisión." });

            int idEmpleado = comisiones.First().IdEmpleado;
            
            await _empleadoAreaComisionService.DeleteByEmpleado(idEmpleado);
            
            // 🔸 Mapear a entidad
            var entidades = _mapper.Map<List<EmpleadoAreaComision>>(comisiones);

            foreach (var item in entidades)
            {
                item.FechaInseccion=DateTime.Now;
                item.IdEmpresa = item.IdEmpresa;
                await _empleadoAreaComisionService.InsertComision(item);
            }
            // 🔸 Reemplazar comisiones del empleado
            

            return Ok(new { message = "Comisiones por área guardadas correctamente ✅" });
        }

        // ✅ PUT: api/EmpleadoAreaComision/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] EmpleadoAreaComisionDto value)
        {
            var comision = await _empleadoAreaComisionService.GetComisionById(id);
            if (comision == null)
                return NotFound(new { message = "Comisión no encontrada" });

            _mapper.Map(value, comision);
            _empleadoAreaComisionService.UpdateComision(id, comision);

            return Ok(new { message = "Comisión actualizada correctamente ✅" });
        }

        // ✅ DELETE: api/EmpleadoAreaComision/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _empleadoAreaComisionService.DeleteComision(id);
            return Ok(new { message = "Comisión eliminada correctamente 🗑️" });
        }

        // ✅ DELETE: api/EmpleadoAreaComision/DeleteByEmpleado/{idEmpleado}
        [HttpDelete("DeleteByEmpleado/{idEmpleado}")]
        public async Task<IActionResult> DeleteByEmpleado(int idEmpleado)
        {
            await _empleadoAreaComisionService.DeleteByEmpleado(idEmpleado);
            return Ok(new { message = "Todas las comisiones del empleado fueron eliminadas 🗑️" });
        }
    }
}
