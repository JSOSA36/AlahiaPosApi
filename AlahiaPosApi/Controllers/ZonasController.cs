using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ZonasController : ControllerBase
    {


        IMapper _Mapper;
        IZonas _IZonasServices;
        IMesas _Mesas;

        public ZonasController(IMapper mapper, IZonas iZonasServices,IMesas mesas)
        {
            _Mapper = mapper;
            _IZonasServices = iZonasServices;
            _Mesas = mesas;
        }



        // GET: api/<ZonasController>
        [HttpGet]
        public async Task<IEnumerable<ZonasDto>> Get()
        {
            var _Zonas= await _IZonasServices.GetAllZonas();
            foreach (var item in _Zonas)
            {
                var _listamesa = await _Mesas.GetAllMesasByZona(item.ZonaId);

                item.Mesas = _listamesa;
            }
           


            return _Mapper.Map<ZonasDto[]>(_Zonas);

        }

        // GET api/<ZonasController>/5
        [HttpGet("{id}")]
        public async Task<ZonasDto> Get(int id)
        {
            var Result = await _IZonasServices.GetAllZonasById(id);

            return _Mapper.Map<ZonasDto>(Result);
        }

        // POST api/<ZonasController>
        [HttpPost]
        public async Task Post([FromBody] ZonasDto value)
        {
            var result = _Mapper.Map<Zonas>(value);
            await _IZonasServices.InsertZonas(result);
        }

        // PUT api/<ZonasController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            var result = _Mapper.Map<Zonas>(value);
             _IZonasServices.UpdateZonas(id,result);
        }

        // DELETE api/<ZonasController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _IZonasServices.DeleteZonas(id);
        }
    }
}
