using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using PrinterLibrary;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriasController : ControllerBase
    {

        IMapper _Mapper;
        ICategorias _services;
        IAreas Areas;
       

        public CategoriasController(IMapper mapper, ICategorias services, IAreas areas)
        {
            _Mapper = mapper;
            _services = services;
            Areas = areas;
        }



        // GET: api/<CategoriasController>
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<Categorias>> Get(int IdEmpresa)
        {
             return await _services.GetAllCategorias(IdEmpresa);
        }
        [HttpGet]
        [Route("GetCategoriaVenta/{IdEmpresa}")]
        public async Task<IEnumerable<Categorias>> GetCategoriaVenta(int IdEmpresa)
        {
            return await _services.GetAllCategoriasVentas(IdEmpresa);
        }

        // GET api/<CategoriasController>/5
        [HttpGet]
        [Route("GetbyId/{id}")]
        public async Task<Categorias> GetbyId(int id)
        {
            return await _services.GetAllCategoriasById(id);
        }

        // POST api/<CategoriasController>
        [HttpPost]
        public async Task Post([FromForm] CategoriaDto value)
        {
            Categorias c = new Categorias();
            c.Nombre = value.Nombre;
            c.Prioridad = 0;
            c.IdEmpresa = value.IdEmpresa;
            c.IsActiva = value.IsActiva;
            c.TipoOperacion = value.TipoOperacion;

            if (value.Imagen != null)
            {
                using var ms = new MemoryStream();
                await value.Imagen.CopyToAsync(ms);
                byte[] imagenBytes = ms.ToArray();

                c.ImagenPath = Utility.UploadFileFtp(
                    imagenBytes,
                    Guid.NewGuid().ToString() + ".jpg"
                );
            }

            // 1️⃣ Guardar categoría
             await _services.InsertCategorias(c);

            // 2️⃣ Crear área automáticamente
            Area area = new Area();
            area.Nombre = c.Nombre;
            area.IdEmpresa = c.IdEmpresa;
            

            await Areas.InsertArea(area);
        }

        // PUT api/<CategoriasController>/5
        [HttpPut]
        public async Task  Put([FromForm] CategoriaDto value)
        {
            var _Cat = await _services.GetAllCategoriasById(value.IdCategoria);
            if (value.Imagen!=null)
            {
                using var ms = new MemoryStream();
                await value.Imagen.CopyToAsync(ms);
                byte[] imagenBytes = ms.ToArray();

                _Cat.ImagenPath = Utility.UploadFileFtp(imagenBytes,
                Guid.NewGuid().ToString() + ".jpg");
            }
            
            
            _Cat.Nombre = value.Nombre;

            _Cat.IsActiva = value.IsActiva;


            _Cat.Nombre = value.Nombre;
            _Cat.Prioridad = 0;
            _Cat.IdEmpresa = value.IdEmpresa;
            _Cat.IsActiva = value.IsActiva;
            _Cat.TipoOperacion = value.TipoOperacion;

            _services.UpdateCategorias(value.IdCategoria, _Cat);
        }

        // DELETE api/<CategoriasController>/5
        [HttpDelete("{id}")]
        public async Task  Delete(int id)
        {
            var _Cat = await _services.GetAllCategoriasById(id);
            _Cat.IsActiva = false;

            _services.UpdateCategorias(id,_Cat);


        }
    }
}
