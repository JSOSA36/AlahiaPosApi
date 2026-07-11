using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlantillasDocumentosClinicosController : ControllerBase
    {
        private readonly IPlantillasDocumentosClinicos _service;
        private readonly IMapper _mapper;

        public PlantillasDocumentosClinicosController(
            IPlantillasDocumentosClinicos service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet("empresa/{idEmpresa}")]
        public async Task<IActionResult> GetByEmpresa(int idEmpresa)
        {
            var plantillas = await _service.GetByEmpresa(idEmpresa);
            return Ok(plantillas);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var plantilla = await _service.GetById(id);

            if (plantilla == null)
                return NotFound(new { message = "Plantilla no encontrada" });

            return Ok(plantilla);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] PlantillaDocumentoClinicoDto dto)
        {
            try
            {
                var plantilla = _mapper.Map<PlantillasDocumentosClinicos>(dto);
                await _service.Insert(plantilla);

                return Ok(new
                {
                    message = "Plantilla creada correctamente",
                    idPlantilla = plantilla.IdPlantilla
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al crear plantilla",
                    error = ex.Message
                });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] PlantillaDocumentoClinicoDto dto)
        {
            try
            {
                var existente = await _service.GetById(dto.IdPlantilla);

                if (existente == null)
                    return NotFound(new { message = "Plantilla no encontrada" });

                var plantilla = _mapper.Map<PlantillasDocumentosClinicos>(dto);
                await _service.Update(dto.IdPlantilla, plantilla);

                return Ok(new { message = "Plantilla actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al actualizar plantilla",
                    error = ex.Message
                });
            }
        }

        [HttpPut("Activar/{id}/{activa}")]
        public async Task<IActionResult> ActivarDesactivar(int id, bool activa)
        {
            try
            {
                await _service.CambiarEstado(id, activa);

                return Ok(new
                {
                    message = activa
                        ? "Plantilla activada correctamente"
                        : "Plantilla desactivada correctamente"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al cambiar estado de la plantilla",
                    error = ex.Message
                });
            }
        }
    }
}
