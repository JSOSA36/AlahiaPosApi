using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlmacenesController : ControllerBase
    {
        private readonly IAlmacenes _almacenes;
        private readonly ISucursalService _sucursales;

        public AlmacenesController(IAlmacenes almacenes, ISucursalService sucursales)
        {
            _almacenes = almacenes;
            _sucursales = sucursales;
        }

        [HttpGet("{idEmpresa}")]
        public async Task<IEnumerable<Almacen>> Get(
            int idEmpresa,
            [FromQuery] bool incluirOtrasSucursales = false,
            [FromQuery] int? idSucursal = null)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null)
            {
                return await _almacenes.GetAllAlmacenes(idEmpresa);
            }

            if (incluirOtrasSucursales)
            {
                var permitidas = await _sucursales.ListarPorUsuarioAsync(
                    sesion.IdUsuario,
                    sesion.IdEmpresa);
                var ids = permitidas.Select(x => x.IdSucursal).ToList();
                return await _almacenes.GetAllAlmacenes(
                    idEmpresa,
                    idsSucursalesPermitidas: ids);
            }

            var sucursalFiltro = idSucursal is > 0 ? idSucursal.Value : sesion.IdSucursal;
            if (sucursalFiltro > 0)
            {
                if (!await _sucursales.TieneAccesoAsync(
                    sesion.IdUsuario,
                    sesion.IdEmpresa,
                    sucursalFiltro))
                {
                    return Enumerable.Empty<Almacen>();
                }

                return await _almacenes.GetAllAlmacenes(idEmpresa, sucursalFiltro);
            }

            return await _almacenes.GetAllAlmacenes(idEmpresa);
        }

        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var almacen = await _almacenes.GetAlmacenById(id);

            if (almacen == null)
            {
                return NotFound();
            }

            var empresa = TenantRecurso.RechazarSiOtraEmpresa(HttpContext, almacen.IdEmpresa);
            if (empresa != null) return empresa;

            if (!await UsuarioPuedeVerSucursalAsync(almacen.IdSucursal))
            {
                return NotFound();
            }

            return Ok(almacen);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Almacen almacen)
        {
            if (almacen == null)
            {
                return BadRequest("El almacén no puede ser nulo.");
            }

            if (string.IsNullOrWhiteSpace(almacen.Nombre))
            {
                return BadRequest("El nombre es obligatorio.");
            }

            if (almacen.IdEmpresa <= 0)
            {
                return BadRequest("La empresa es obligatoria.");
            }

            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion != null)
            {
                almacen.IdEmpresa = sesion.IdEmpresa;
                if (almacen.IdSucursal is null or <= 0)
                    almacen.IdSucursal = sesion.IdSucursal > 0 ? sesion.IdSucursal : null;
                almacen.IdUsuarioCreacion = sesion.IdUsuario;
            }

            if (almacen.IdSucursal is > 0
                && sesion != null
                && !await _sucursales.TieneAccesoAsync(
                    sesion.IdUsuario,
                    sesion.IdEmpresa,
                    almacen.IdSucursal.Value))
            {
                return StatusCode(403, new { message = SesionHttp.ForbiddenOtraSucursal });
            }

            almacen.Nombre = almacen.Nombre.Trim();
            almacen.Descripcion = almacen.Descripcion?.Trim();
            almacen.FechaCreacion = DateTime.Now;
            almacen.Activo = true;

            await _almacenes.InsertAlmacen(almacen);

            return Ok(almacen);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Almacen almacen)
        {
            if (almacen == null || id != almacen.IdAlmacen)
            {
                return BadRequest("Datos inválidos.");
            }

            if (string.IsNullOrWhiteSpace(almacen.Nombre))
            {
                return BadRequest("El nombre es obligatorio.");
            }

            var existente = await _almacenes.GetAlmacenById(id);

            if (existente == null)
            {
                return NotFound();
            }

            var empresa = TenantRecurso.RechazarSiOtraEmpresa(HttpContext, existente.IdEmpresa);
            if (empresa != null) return empresa;

            if (!await UsuarioPuedeVerSucursalAsync(existente.IdSucursal))
            {
                return NotFound();
            }

            existente.Nombre = almacen.Nombre.Trim();
            existente.Descripcion = almacen.Descripcion?.Trim();
            existente.EsPrincipal = almacen.EsPrincipal;
            existente.Activo = almacen.Activo;

            await _almacenes.UpdateAlmacen(id, existente);

            return Ok(existente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existente = await _almacenes.GetAlmacenById(id);
            if (existente == null)
            {
                return NoContent();
            }

            var empresa = TenantRecurso.RechazarSiOtraEmpresa(HttpContext, existente.IdEmpresa);
            if (empresa != null) return empresa;

            if (!await UsuarioPuedeVerSucursalAsync(existente.IdSucursal))
            {
                return NotFound();
            }

            await _almacenes.DeleteAlmacen(id);
            return NoContent();
        }

        private async Task<bool> UsuarioPuedeVerSucursalAsync(int? idSucursal)
        {
            var sesion = SesionHttp.TryGet(HttpContext);
            if (sesion == null || idSucursal is null or <= 0)
                return true;

            return await _sucursales.TieneAccesoAsync(
                sesion.IdUsuario,
                sesion.IdEmpresa,
                idSucursal.Value);
        }
    }
}
