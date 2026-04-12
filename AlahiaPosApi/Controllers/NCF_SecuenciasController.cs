using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NCF_SecuenciasController : ControllerBase
    {
        private readonly INCF_Secuencias _INCF_Secuencias;

        public NCF_SecuenciasController(INCF_Secuencias iNCF_Secuencias)
        {
            _INCF_Secuencias = iNCF_Secuencias;
        }

        // GET: api/NCF_Secuencias/1
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<SecuenciaECF>> Get(int IdEmpresa)
        {
            return await _INCF_Secuencias.GetAll(IdEmpresa);
        }

        // GET: api/NCF_Secuencias/GetById/5
        [HttpGet("GetById/{id}")]
        public async Task<SecuenciaECF> GetById(int id)
        {
            return await _INCF_Secuencias.GetById(id);
        }

        // POST: api/NCF_Secuencias
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SecuenciaECF secuencia)
        {
            if (secuencia == null)
                return BadRequest("La secuencia no puede ser nula");
            secuencia.Empresa = null;
            await _INCF_Secuencias.Insert(secuencia);
            return Ok(secuencia);
        }

        // PUT: api/NCF_Secuencias/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] SecuenciaECF secuencia)
        {
            if (secuencia == null || id != secuencia.IdSecuencia)
                return BadRequest("Datos inválidos");

            _INCF_Secuencias.Update(secuencia);
            return NoContent();
        }

        // DELETE: api/NCF_Secuencias/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _INCF_Secuencias.Delete(id);
            return NoContent();
        }

        // 🔥 POST: api/NCF_Secuencias/Generar
        [HttpPost("Generar")]
        public async Task<IActionResult> GenerarNCF([FromQuery] int idEmpresa, [FromQuery] string tipoNCF)
        {
            try
            {
                var ncf = await _INCF_Secuencias.GenerarNCF(idEmpresa, tipoNCF);
                return Ok(new { NCF = ncf });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}