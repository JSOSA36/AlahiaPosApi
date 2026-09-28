using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {

        IClientes _IClientes;
        IMapper _Mapper;
        public ClientesController(IClientes iClientes,IMapper mapper)
        {
            _IClientes = iClientes;
            _Mapper = mapper;
        }

        [HttpGet("Buscar/{IdEmpresa}/{nombre}")]
        public async Task<IEnumerable<Clientes>> Buscar(int IdEmpresa, string nombre)
        {
            return await _IClientes.BuscarPorNombre(nombre, IdEmpresa);
        }

        [HttpGet("por-telefono")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByTelefono(
        [FromQuery] string telefono,
        [FromQuery] int idEmpresa
        )
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return BadRequest("Teléfono requerido");

            var cliente = await _IClientes.BuscarPorTelefono(telefono, idEmpresa);

            if (cliente == null)
                return Ok(null); // 👈 CLAVE

            var tel =
                !string.IsNullOrWhiteSpace(cliente.Celular) ? cliente.Celular
                : (cliente.Telefono ?? "");

            return Ok(new
            {
                idCliente = cliente.IDCliente,
                nombre = cliente.NombreComercial,
                nombreComercial = cliente.NombreComercial,
                telefono = tel,
                celular = tel,
                correo = cliente.Email
            });
        }

        // GET: api/<ClientesController>
        [HttpGet("{IdEmpresa}")]
        public async Task<IEnumerable<Clientes>> Get(int IdEmpresa)
        {
            
                return await _IClientes.GetAllClientes(IdEmpresa);
            
            
            
        }

        // GET api/<ClientesController>/5
        [HttpGet]
        [Route("GetbyId/{id}")]
        public async Task<IActionResult> GetbyId(int id)
        {
            var cliente = await _IClientes.GetAllClientesById(id);
            if (cliente == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, cliente.IdEmpresa))
                return NotFound();
            return Ok(cliente);
        }

        // POST api/<ClientesController>
        [HttpPost()]
        [AllowAnonymous]
        public async Task<ActionResult> Post(ClienteDto value)
        {
            try
            {
                Clientes c = new Clientes();
                c.CedulaRNC = "00000";
                c.NombreComercial = value.NombreComercial;
                c.Celular = value.Celular;
                c.Email = value.Email;
                c.Direccion = value.Direccion;
                c.FechaInseccion = DateTime.Now.Date;
                c.LimiteCredito = 0;
                c.FechaNacimiento = value.FechaNacimiento;
                c.Estado = true;
                c.IdEmpresa = value.IdEmpresa;

                // 🔥 Insertar cliente
                await _IClientes.InsertClientes(c);

                // 🔥 Retornar cliente insertado (con ID ya generado)
                return Ok(new
                {
                    idCliente = c.IDCliente,
                    nombreComercial = c.NombreComercial,
                    telefono = c.Telefono,
                    celular = c.Celular,
                    email = c.Email,
                    direccion = c.Direccion,
                    fechaNacimiento = c.FechaNacimiento,
                    estado = c.Estado,
                    idEmpresa = c.IdEmpresa
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error al guardar cliente", error = ex.Message });
            }
        }


        // PUT api/<ClientesController>/5
        [HttpPut]
        public async Task<IActionResult> Put(ClienteDto value)
        {
            var _Udate = await _IClientes.GetAllClientesById(value.IdCliente);
            if (_Udate == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, _Udate.IdEmpresa))
                return NotFound();
           
            _Udate.NombreComercial = value.NombreComercial;
            _Udate.Celular = value.Celular;
            _Udate.Email = value.Email;
            _Udate.Direccion = value.Direccion;
            _Udate.Telefono = value.Telefono;

            _Udate.FechaNacimiento = value.FechaNacimiento;
            
            _IClientes.UpdateClientes(value.IdCliente, _Udate);
            return Ok();
        }

        // DELETE api/<ClientesController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _IClientes.GetAllClientesById(id);
            if (cliente == null)
                return NotFound();
            if (!TenantRecurso.EsDeLaSesion(HttpContext, cliente.IdEmpresa))
                return NotFound();
            _IClientes.DeleteClientes(id);
            return Ok();
        }
    }
}
