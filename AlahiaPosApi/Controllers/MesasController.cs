using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MesasController : ControllerBase
    {

        IMapper _Mapper;
        IMesas _Mesa;
       
        public MesasController(IMesas mesas, IMapper mapper)
        {
            _Mapper = mapper;
            _Mesa = mesas;
        }
        // GET: api/<MesasController>
       // [HttpGet]
        //public string<string> Get()
        //{
            //return "";
        //}

        // GET api/<MesasController>/5
        [HttpGet("{IdZona}")]
        public async Task<IEnumerable<Mesas>> Get(int IdZona)
        {
            return await _Mesa.GetAllMesasByZona(IdZona);
        }

        // POST api/<MesasController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<MesasController>/5
        [HttpPut]
        public IActionResult Put([FromBody] Mesas mesa)
        {
            if (mesa == null || mesa.IdMesa <= 0)
            {
                return BadRequest(new { message = "Mesa inválida." });
            }

            _Mesa.UpdateMesas(mesa.IdMesa, mesa);
            return Ok(mesa);
        }

        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Mesas mesa)
        {
            if (mesa == null)
            {
                return BadRequest(new { message = "Mesa inválida." });
            }

            mesa.IdMesa = id;
            _Mesa.UpdateMesas(id, mesa);
            return Ok(mesa);
        }

        // DELETE api/<MesasController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {

        }
    }
}
