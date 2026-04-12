using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImpresorasZonasController : ControllerBase
    {


        IMapper _Mapper;
        IImpresorasZonas _impresorasZonasServices;

        public ImpresorasZonasController(IMapper mapper, IImpresorasZonas iZonasServices)
        {
            _Mapper = mapper;
            _impresorasZonasServices = iZonasServices;
        }


        // GET: api/<ImpresorasZonasController>
        [HttpGet]
        public async  Task<IEnumerable<ImpresorasZonas>> Get()
        {
            return await _impresorasZonasServices.GetImpresorasZonas();
            
        }

        // GET api/<ImpresorasZonasController>/5
        [HttpGet("{id}")]
        public async Task<ImpresorasZonas> Get(int id)
        {
            return await _impresorasZonasServices.GetAllImpresorasZonasById(id);
        }

        // POST api/<ImpresorasZonasController>
        [HttpPost]
        public async Task Post([FromBody] ImpresorasZonas value)
        {
            await _impresorasZonasServices.InsertImpresorasZonas(value);
        }

        // PUT api/<ImpresorasZonasController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] ImpresorasZonas value)
        {
            _impresorasZonasServices.UpdateImpresorasZonas(id, value);
        }

        // DELETE api/<ImpresorasZonasController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _impresorasZonasServices.DeleteImpresorasZonas(id);
        }
    }
}
