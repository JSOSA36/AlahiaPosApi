using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FichaClinicaController : ControllerBase
    {
        private readonly IFichaClinica _service;

        public FichaClinicaController(IFichaClinica service)
        {
            _service = service;
        }

        [HttpGet("{idEmpresa:int}/{idCliente:int}")]
        public async Task<IActionResult> Get(int idEmpresa, int idCliente)
        {
            var vista = await _service.GetVista(idEmpresa, idCliente);
            if (vista == null)
                return NotFound(new { message = "Paciente no encontrado en esta empresa." });

            return Ok(vista);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] FichaClinicaDto dto)
        {
            try
            {
                var vista = await _service.Guardar(dto);
                return Ok(vista);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al guardar la ficha clínica.",
                    error = ex.Message
                });
            }
        }
    }
}
