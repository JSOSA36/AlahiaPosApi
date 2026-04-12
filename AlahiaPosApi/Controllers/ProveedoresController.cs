using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {

        IProveedores _Iproveedores;

        public ProveedoresController(IProveedores iproveedores)
        {
            _Iproveedores = iproveedores;
        }


        // GET: api/<ProveedoresController>
        [HttpGet]
        public async Task<IEnumerable<Proveedores>> Get()
        {
            return await _Iproveedores.GetAllProveedores();
        }

        // GET api/<ProveedoresController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ProveedoresController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ProveedoresController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ProveedoresController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
