using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArsAseguradoraController : ControllerBase
    {
        private readonly IArsAseguradoraService _ars;

        public ArsAseguradoraController(IArsAseguradoraService ars)
        {
            _ars = ars;
        }

        [HttpGet("{idEmpresa:int}")]
        public async Task<IActionResult> Get(int idEmpresa, [FromQuery] bool soloActivos = true)
        {
            return Ok(await _ars.GetAll(idEmpresa, soloActivos));
        }

        [HttpGet("GetById/{id:int}/{idEmpresa:int}")]
        public async Task<IActionResult> GetById(int id, int idEmpresa)
        {
            var ars = await _ars.GetById(id, idEmpresa);
            if (ars == null)
                return NotFound(new { message = "ARS no encontrada." });
            return Ok(ars);
        }

        [HttpGet("EmpresaUsaArs/{idEmpresa:int}")]
        public async Task<IActionResult> EmpresaUsaArs(int idEmpresa)
        {
            return Ok(new { activo = await _ars.EmpresaUsaArsAsync(idEmpresa) });
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ArsAseguradoraDto dto)
        {
            try
            {
                var creado = await _ars.Insert(new ArsAseguradora
                {
                    IdEmpresa = dto.IdEmpresa,
                    Nombre = dto.Nombre,
                    RNC = dto.RNC,
                    Telefono = dto.Telefono,
                    Direccion = dto.Direccion,
                    Email = dto.Email,
                    Contacto = dto.Contacto,
                    Observaciones = dto.Observaciones,
                    Activo = true
                });
                return Ok(creado);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] ArsAseguradoraDto dto)
        {
            try
            {
                await _ars.Update(id, new ArsAseguradora
                {
                    Nombre = dto.Nombre,
                    RNC = dto.RNC,
                    Telefono = dto.Telefono,
                    Direccion = dto.Direccion,
                    Email = dto.Email,
                    Contacto = dto.Contacto,
                    Observaciones = dto.Observaciones,
                    Activo = dto.Activo
                }, dto.IdEmpresa);
                return Ok(new { success = true, message = "ARS actualizada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Activar/{id:int}/{idEmpresa:int}")]
        public async Task<IActionResult> Activar(int id, int idEmpresa)
        {
            try
            {
                await _ars.SetActivo(id, idEmpresa, true);
                return Ok(new { success = true, message = "ARS activada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Desactivar/{id:int}/{idEmpresa:int}")]
        public async Task<IActionResult> Desactivar(int id, int idEmpresa)
        {
            try
            {
                await _ars.SetActivo(id, idEmpresa, false);
                return Ok(new { success = true, message = "ARS desactivada." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("CuentasPorCobrar/{idEmpresa:int}")]
        public async Task<IActionResult> CuentasPorCobrar(
            int idEmpresa,
            [FromQuery] int idArs = 0,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int idSucursal = 0)
        {
            var data = await _ars.GetCuentasPorCobrarArsAsync(new ArsResumenFiltroRequest
            {
                IdEmpresa = idEmpresa,
                IdArs = idArs,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                IdSucursal = idSucursal
            });
            return Ok(data);
        }

        [HttpGet("Documentos/{idEmpresa:int}")]
        public async Task<IActionResult> Documentos(
            int idEmpresa,
            [FromQuery] int idArs = 0,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int idSucursal = 0)
        {
            var data = await _ars.GetDocumentosArsAsync(new ArsResumenFiltroRequest
            {
                IdEmpresa = idEmpresa,
                IdArs = idArs,
                Estado = estado,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
                IdSucursal = idSucursal
            });
            return Ok(data);
        }

        [HttpGet("Pagos/{idEmpresa:int}")]
        public async Task<IActionResult> Pagos(
            int idEmpresa,
            [FromQuery] int idFacturaHeader = 0,
            [FromQuery] int idArs = 0)
        {
            return Ok(await _ars.GetPagosArsAsync(idEmpresa, idFacturaHeader, idArs));
        }

        [HttpGet("Ventas/{idEmpresa:int}")]
        public async Task<IActionResult> Ventas(
            int idEmpresa,
            [FromQuery] int idArs = 0,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            var data = await _ars.GetVentasPorArsAsync(new ArsResumenFiltroRequest
            {
                IdEmpresa = idEmpresa,
                IdArs = idArs,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta
            });
            return Ok(data);
        }

        [HttpGet("Antiguedad/{idEmpresa:int}")]
        public async Task<IActionResult> Antiguedad(
            int idEmpresa,
            [FromQuery] int idArs = 0)
        {
            var data = await _ars.GetAntiguedadArsAsync(new ArsResumenFiltroRequest
            {
                IdEmpresa = idEmpresa,
                IdArs = idArs
            });
            return Ok(data);
        }

        [HttpGet("DesgloseCaja/{idEmpresa:int}/{idUsuario:int}")]
        public async Task<IActionResult> DesgloseCaja(int idEmpresa, int idUsuario)
        {
            return Ok(await _ars.GetDesgloseCajaAbiertaAsync(idEmpresa, idUsuario));
        }

        [HttpPost("RegistrarPago")]
        public async Task<IActionResult> RegistrarPago([FromBody] ArsPagoRequest request)
        {
            try
            {
                await _ars.RegistrarPagoArsAsync(request);
                return Ok(new { success = true, message = "Pago de ARS registrado." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("RegistrarPagoLote")]
        public async Task<IActionResult> RegistrarPagoLote([FromBody] ArsPagoLoteRequest request)
        {
            try
            {
                var resultado = await _ars.RegistrarPagoLoteArsAsync(request);
                return Ok(new
                {
                    success = true,
                    message = $"Se cotejaron {resultado.Documentos} factura(s) de {resultado.NombreArs}.",
                    resultado
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
