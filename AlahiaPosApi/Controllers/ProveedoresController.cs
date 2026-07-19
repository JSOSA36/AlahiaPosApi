using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {
        private readonly IProveedores _proveedores;

        public ProveedoresController(IProveedores proveedores)
        {
            _proveedores = proveedores;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<Proveedores>> Get(int idEmpresa, [FromQuery] bool soloActivos = true)
        {
            return await _proveedores.GetAllProveedores(idEmpresa, soloActivos);
        }

        [HttpGet("GetbyId/{id}/{idEmpresa}")]
        public async Task<IActionResult> GetById(int id, int idEmpresa)
        {
            var proveedor = await _proveedores.GetProveedorById(id, idEmpresa);
            if (proveedor == null)
                return NotFound(new { message = "Proveedor no encontrado." });

            return Ok(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ProveedorDto dto)
        {
            try
            {
                var proveedor = new Proveedores
                {
                    IdEmpresa = dto.IdEmpresa,
                    RNC = dto.RNC,
                    NombreComercial = dto.NombreComercial,
                    Telefono = dto.Telefono,
                    Direccion = dto.Direccion,
                    Email = dto.Email,
                    Nota = dto.Nota,
                    IsActivo = true
                };

                var creado = await _proveedores.InsertProveedores(proveedor);
                return Ok(creado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] ProveedorDto dto)
        {
            try
            {
                var proveedor = new Proveedores
                {
                    RNC = dto.RNC,
                    NombreComercial = dto.NombreComercial,
                    Telefono = dto.Telefono,
                    Direccion = dto.Direccion,
                    Email = dto.Email,
                    Nota = dto.Nota,
                    IsActivo = dto.IsActivo
                };

                await _proveedores.UpdateProveedores(id, proveedor, dto.IdEmpresa);
                return Ok(new { success = true, message = "Proveedor actualizado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Desactivar/{id}/{idEmpresa}")]
        public async Task<IActionResult> Desactivar(int id, int idEmpresa)
        {
            try
            {
                await _proveedores.DesactivarProveedor(id, idEmpresa);
                return Ok(new { success = true, message = "Proveedor desactivado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("TieneDocumentos/{id}")]
        public async Task<IActionResult> TieneDocumentos(int id)
        {
            var tiene = await _proveedores.TieneDocumentosAsociados(id);
            return Ok(new { tieneDocumentos = tiene });
        }
    }
}
