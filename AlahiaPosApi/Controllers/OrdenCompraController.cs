using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenCompraController : ControllerBase
    {

        IMapper _Mapper;
        IOrdenCompraHeader _IOrdenCompraHeader;

        public OrdenCompraController(IMapper mapper, 
            IOrdenCompraHeader iOrdenCompraHeader)
        {
            _Mapper = mapper;
            _IOrdenCompraHeader = iOrdenCompraHeader;
        }
        [HttpGet]
        [Route("CuentaxPagar")]
        public async Task<decimal> CuentaxPagar()
        {
            return await _IOrdenCompraHeader.GetCuentaxPagar();
        }
        [HttpGet]
        [Route("GetFacturasxPagar")]
        public async Task<IEnumerable<OrdenCompraHeader>> GetFacturasxPagar
            (DateTime? Desde, DateTime? Hasta, int IdProveedor)
        {
            return await _IOrdenCompraHeader.GetFacturasxPagar(Desde,Hasta,IdProveedor);
        }

        // GET: api/<OrdenCompraController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<OrdenCompraController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<OrdenCompraController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<OrdenCompraController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<OrdenCompraController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
