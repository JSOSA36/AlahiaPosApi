using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentosClinicosController : ControllerBase
    {
        private readonly IDocumentosClinicos _service;
        private readonly IMapper _mapper;

        public DocumentosClinicosController(
            IDocumentosClinicos service,
            IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet("empresa/{idEmpresa}")]
        public async Task<IActionResult> GetByEmpresa(int idEmpresa)
        {
            var documentos = await _service.GetByEmpresa(idEmpresa);
            return Ok(MapList(documentos));
        }

        [HttpGet("cliente/{idEmpresa}/{idCliente}")]
        public async Task<IActionResult> GetByCliente(int idEmpresa, int idCliente)
        {
            var documentos = await _service.GetByCliente(idEmpresa, idCliente);
            return Ok(MapList(documentos));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var documento = await _service.GetById(id);

            if (documento == null)
                return NotFound(new { message = "Documento clínico no encontrado" });

            return Ok(MapItem(documento));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] DocumentoClinicoDto dto)
        {
            try
            {
                var documento = _mapper.Map<DocumentosClinicos>(dto);
                await _service.Insert(documento);

                return Ok(new
                {
                    message = "Documento clínico creado correctamente",
                    idDocumentoClinico = documento.IdDocumentoClinico
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al crear documento clínico",
                    error = ex.Message
                });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] DocumentoClinicoDto dto)
        {
            try
            {
                var existente = await _service.GetById(dto.IdDocumentoClinico);

                if (existente == null)
                    return NotFound(new { message = "Documento clínico no encontrado" });

                var documento = _mapper.Map<DocumentosClinicos>(dto);
                await _service.Update(dto.IdDocumentoClinico, documento);

                return Ok(new { message = "Documento clínico actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al actualizar documento clínico",
                    error = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existente = await _service.GetById(id);

                if (existente == null)
                    return NotFound(new { message = "Documento clínico no encontrado" });

                await _service.Delete(id);

                return Ok(new { message = "Documento clínico eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al eliminar documento clínico",
                    error = ex.Message
                });
            }
        }

        private IEnumerable<DocumentoClinicoDto> MapList(
            IEnumerable<DocumentosClinicos> documentos)
        {
            return documentos.Select(MapItem);
        }

        private DocumentoClinicoDto MapItem(DocumentosClinicos documento)
        {
            var dto = _mapper.Map<DocumentoClinicoDto>(documento);
            dto.NombreCliente = documento.Cliente?.NombreComercial;
            return dto;
        }
    }
}
