using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GastosController : ControllerBase
    {
        IGastos _IGastos;

        public GastosController(IGastos iGastos)
        {
            _IGastos = iGastos;
        }


        // GET: api/<GastosController>
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<Gastos>> Get(int IdEmpresa)
        {
            return await _IGastos.GetAllGastos(IdEmpresa);
        }

        // GET api/<GastosController>/5
        

        // POST api/<GastosController>
        [HttpPost]
        public async Task Post([FromBody] Gastos value)
        {
            value.IdEmpresa = value.IdEmpresa;
            value.FechaInseccion = DateTime.Now.Date;
            value.IdProveedor = 1;
            await _IGastos.InsertGastos(value);
        }

        // PUT api/<GastosController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<GastosController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _IGastos.DeleteGastos(id);
        }
        [HttpGet]
        [Route("GetTotalGastos")]
        public async Task<decimal> GetTotalGastos(int IdEmpresa)
        {
            return await _IGastos.TotalGastosDelMes(IdEmpresa);
        }
    }
}
