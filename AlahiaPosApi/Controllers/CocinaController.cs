using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CocinaController : ControllerBase
    {


        IMapper _Mapper;
        ICocinas _services;


        public CocinaController(IMapper mapper, ICocinas services)
        {
            _Mapper = mapper;
            _services = services;

        }

        // GET: api/<CocinaController>
        [HttpGet]
        public async Task<IEnumerable<Cocinas>> Get()
        {
            return await _services.GetAllCocinas();
        }


        // GET api/<CocinaController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<CocinaController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<CocinaController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<CocinaController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
