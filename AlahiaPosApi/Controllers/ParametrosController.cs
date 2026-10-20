using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParametrosController : ControllerBase
    {
        private readonly IParametrosService _ParametrosService;
        private readonly IMapper _Mapper;

        public ParametrosController(IParametrosService parametrosService, IMapper mapper)
        {
            _ParametrosService = parametrosService;
            _Mapper = mapper;
        }

        // 🔹 GET: api/Parametros/empresa/1
        [HttpGet("GetParametrosEmpresa/{idEmpresa}")]
        public async Task<IEnumerable<Parametros>> GetParametrosEmpresa(int idEmpresa)
        {
            return await _ParametrosService.GetParametrosEmpresa(idEmpresa);
        }

        // 🔹 GET: api/Parametros/pos/1/POS01
        [HttpGet("GetParametrosPOS/{idEmpresa}/{codigoPOS}")]
        public async Task<IEnumerable<Parametros>> GetParametrosPOS(int idEmpresa, string codigoPOS)
        {
            return await _ParametrosService.GetParametrosPOS(idEmpresa, codigoPOS);
        }

        // 🔹 GET: api/Parametros/empresa/1/ManejaMesas
        [HttpGet("GetParametro/{idEmpresa}/{clave}")]
        public async Task<IActionResult> GetParametro(int idEmpresa, string clave)
        {
            var parametro = await _ParametrosService.GetParametro(idEmpresa, clave);

            if (parametro == null)
                return NotFound(new { message = "Parámetro no encontrado" });

            return Ok(parametro);
        }

        // 🔹 GET: api/Parametros/pos/1/POS01/Impresora
        [HttpGet("GetParametroPOS/{idEmpresa}/{codigoPOS}/{clave}")]
        public async Task<IActionResult> GetParametroPOS(int idEmpresa, string codigoPOS, string clave)
        {
            var parametro = await _ParametrosService.GetParametroPOS(idEmpresa, codigoPOS, clave);

            if (parametro == null)
                return NotFound(new { message = "Parámetro POS no encontrado" });

            return Ok(parametro);
        }
        [HttpPost("GuardarLista")]
        public async Task<IActionResult> GuardarLista([FromBody] List<ParametrosDto> parametros)
        {
            if (parametros == null || !parametros.Any())
                return BadRequest(new { message = "La lista de parámetros está vacía." });

            var rechazo = await RechazoApagadoItbisAsync(parametros);
            if (rechazo != null)
                return BadRequest(new { message = rechazo });

            try
            {
                foreach (var p in parametros)
                {
                    var parametro = _Mapper.Map<Parametros>(p);

                    Parametros? existente = null;

                    if (!string.IsNullOrWhiteSpace(parametro.CodigoPOS))
                    {
                        existente = await _ParametrosService.GetParametroPOS(
                            parametro.IdEmpresa,
                            parametro.CodigoPOS,
                            parametro.Clave);
                    }
                    else
                    {
                        existente = await _ParametrosService.GetParametro(
                            parametro.IdEmpresa,
                            parametro.Clave);
                    }

                    if (existente != null)
                    {
                        parametro.IdParametro = existente.IdParametro;
                        await _ParametrosService.UpdateParametro(parametro);
                    }
                    else
                    {
                        await _ParametrosService.InsertParametro(parametro);
                    }
                }

                return Ok(new { message = "Parámetros guardados correctamente ✅" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al guardar parámetros",
                    error = ex.Message
                });
            }
        }
        // 🔹 POST: api/Parametros
        [HttpPost]
        public async Task<IActionResult> Post([FromForm] ParametrosDto value)
        {
            var rechazo = await RechazoApagadoItbisAsync(new List<ParametrosDto> { value });
            if (rechazo != null)
                return BadRequest(new { message = rechazo });

            try
            {
                var parametro = _Mapper.Map<Parametros>(value);

                await _ParametrosService.InsertParametro(parametro);

                return Ok(new { message = "✅ Parámetro creado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al crear parámetro",
                    error = ex.Message
                });
            }
        }

        // 🔹 PUT: api/Parametros
        [HttpPut]
        public async Task<IActionResult> Put([FromForm] ParametrosDto value)
        {
            var rechazo = await RechazoApagadoItbisAsync(new List<ParametrosDto> { value });
            if (rechazo != null)
                return BadRequest(new { message = rechazo });

            try
            {
                var parametro = _Mapper.Map<Parametros>(value);

                await _ParametrosService.UpdateParametro(parametro);

                return Ok(new { message = "✅ Parámetro actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al actualizar parámetro",
                    error = ex.Message
                });
            }
        }

        private async Task<string?> RechazoApagadoItbisAsync(List<ParametrosDto> parametros)
        {
            foreach (var grupo in parametros.GroupBy(p => p.IdEmpresa))
            {
                var lista = grupo.ToList();
                var politicaEnLista = lista.FirstOrDefault(p =>
                    string.Equals((p.Clave ?? "").Trim(), PoliticaFiscalAutomatica.ClaveParametro, StringComparison.OrdinalIgnoreCase));

                bool politicaActiva;
                if (politicaEnLista != null)
                    politicaActiva = PoliticaFiscalAutomatica.ValorActivo(politicaEnLista.Valor);
                else
                {
                    var guardada = await _ParametrosService.GetParametro(
                        grupo.Key,
                        PoliticaFiscalAutomatica.ClaveParametro);
                    politicaActiva = guardada != null
                        && guardada.Activo
                        && PoliticaFiscalAutomatica.ValorActivo(guardada.Valor);
                }

                if (!politicaActiva)
                    continue;

                foreach (var p in lista)
                {
                    if (!string.Equals((p.Clave ?? "").Trim(), PoliticaFiscalAutomatica.ClaveFacturarConItbis, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var actual = await _ParametrosService.GetParametro(grupo.Key, PoliticaFiscalAutomatica.ClaveFacturarConItbis);
                    var estabaActivo = actual != null
                        && actual.Activo
                        && PoliticaFiscalAutomatica.ValorActivo(actual.Valor);
                    if (PoliticaFiscalAutomatica.BloqueaApagadoItbisGlobal(politicaActiva, estabaActivo, p.Clave, p.Valor))
                        return "La política fiscal automática no permite desactivar el ITBIS de la empresa.";
                }
            }

            return null;
        }

        // 🔹 DELETE: api/Parametros/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _ParametrosService.DeleteParametro(id);

                return Ok(new { message = "✅ Parámetro eliminado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error al eliminar parámetro",
                    error = ex.Message
                });
            }
        }
    }
}