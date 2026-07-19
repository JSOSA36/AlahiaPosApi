using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresaModulosController : ControllerBase
    {
        private readonly IEmpresaModulos _IEmpresaModulos;
        private readonly IContabilidadConfiguracionService _contabilidadConfiguracion;
        private readonly IMapper _Mapper;

        public EmpresaModulosController(
            IEmpresaModulos empresaModulos,
            IContabilidadConfiguracionService contabilidadConfiguracion,
            IMapper mapper)
        {
            _IEmpresaModulos = empresaModulos;
            _contabilidadConfiguracion = contabilidadConfiguracion;
            _Mapper = mapper;
        }

        [HttpGet("GetByEmpresa/{idEmpresa}")]
        public async Task<IEnumerable<EmpresaModulo>> GetByEmpresa(int idEmpresa)
        {
            return await _IEmpresaModulos.GetModulosByEmpresa(idEmpresa);
        }

        [HttpGet("TieneModulo/{idEmpresa}/{idModulo}")]
        public async Task<IActionResult> TieneModulo(int idEmpresa, int idModulo)
        {
            var tiene = await _IEmpresaModulos.EmpresaTieneModulo(idEmpresa, idModulo);
            return Ok(new { tiene });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] EmpresaModuloDto value)
        {
            try
            {
                var entity = _Mapper.Map<EmpresaModulo>(value);
                await _IEmpresaModulos.InsertEmpresaModulo(entity);

                await _contabilidadConfiguracion.InicializarSiEsModuloContabilidadAsync(
                    entity.EmpresaId,
                    entity.ModuloId,
                    entity.Activo);

                return Ok(new { message = "✅ Módulo asignado a la empresa correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error asignando módulo", error = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromForm] EmpresaModuloDto value)
        {
            var entity = _Mapper.Map<EmpresaModulo>(value);
            _IEmpresaModulos.UpdateEmpresaModulo(value.Id, entity);

            await _contabilidadConfiguracion.InicializarSiEsModuloContabilidadAsync(
                entity.EmpresaId,
                entity.ModuloId,
                entity.Activo);

            return Ok(new { message = "✅ Módulo actualizado correctamente" });
        }
    }
}
