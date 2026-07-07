using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BizcochoEncargoController : ControllerBase
    {
        private readonly IBizcochoEncargoService _service;

        public BizcochoEncargoController(IBizcochoEncargoService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("idEmpresa es obligatorio");

            var data = await _service.GetAllAsync(idEmpresa);
            return Ok(data);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("idEmpresa es obligatorio");

            var data = await _service.GetByIdAsync(id, idEmpresa);
            if (data == null)
                return NotFound(new { message = "Encargo no encontrado" });

            return Ok(data);
        }

        [HttpGet("pendientes")]
        public async Task<IActionResult> GetPendientes([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest("idEmpresa es obligatorio");

            var data = await _service.GetPendientesAsync(idEmpresa);
            return Ok(data);
        }

        [HttpGet("fecha")]
        public async Task<IActionResult> GetPorFecha(
            [FromQuery] int idEmpresa,
            [FromQuery] DateTime fecha)
        {
            if (idEmpresa <= 0)
                return BadRequest("idEmpresa es obligatorio");

            var data = await _service.GetPorFechaAsync(idEmpresa, fecha);
            return Ok(data);
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar(
            [FromQuery] int idEmpresa,
            [FromQuery] string filtro)
        {
            if (idEmpresa <= 0)
                return BadRequest("idEmpresa es obligatorio");

            var data = await _service.BuscarAsync(idEmpresa, filtro);
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RequestBizcochoEncargoDto request)
        {
            try
            {
                var id = await _service.CreateAsync(request);
                var idEmpresa = request?.Encargo?.IdEmpresa ?? 0;

                return CreatedAtAction(
                    nameof(GetById),
                    new { id, idEmpresa },
                    new { message = "Encargo creado correctamente", id });
            }
            catch (Exception ex)
            {
                return MapException(ex);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateBizcochoEncargoDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Datos inválidos" });

            if (dto.IdFacturaHeader != id)
                return BadRequest(new { message = "El id de la ruta no coincide con el cuerpo" });

            if (dto.IdEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            try
            {
                var updated = await _service.UpdateAsync(dto);
                if (!updated)
                    return NotFound(new { message = "Encargo no encontrado" });

                return Ok(new { message = "Encargo actualizado" });
            }
            catch (Exception ex)
            {
                return MapException(ex);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            var deleted = await _service.DeleteAsync(id, idEmpresa);
            if (!deleted)
                return NotFound(new { message = "Encargo no encontrado" });

            return Ok(new { message = "Encargo eliminado" });
        }

        [HttpPost("pagar")]
        public async Task<IActionResult> Pagar(
            
            [FromBody] PagoEncargoDto dto)
        {
            if (dto.IdEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            if (dto == null)
                return BadRequest(new { message = "Datos inválidos" });

            try
            {
                await _service.RegistrarPagoAsync(
                    dto.IdEmpresa,
                    dto.IdEncargo,
                    dto.Monto,
                    dto.Itbis,
                    dto.TotalPago,
                    dto.FormaPago,
                    dto.TipoComprobante,
                    dto.RNC,
                    dto.NombreEmpresa);

                return Ok(new { message = "Pago aplicado correctamente" });
            }
            catch (Exception ex)
            {
                return MapException(ex);
            }
        }

        [HttpPost("entregar")]
        public async Task<IActionResult> Entregar(
            [FromQuery] int idEncargo,
            [FromQuery] int idEmpresa)
        {
            if (idEncargo <= 0 || idEmpresa <= 0)
                return BadRequest(new { message = "idEncargo e idEmpresa son obligatorios" });

            var entregado = await _service.MarcarComoEntregadoAsync(idEncargo, idEmpresa);
            if (!entregado)
                return NotFound(new { message = "Encargo no encontrado" });

            return Ok(new { message = "Encargo entregado" });
        }

        [HttpGet("total-hoy")]
        public async Task<IActionResult> GetTotalHoy([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            var total = await _service.GetTotalCobradoHoyAsync(idEmpresa);
            return Ok(total);
        }

        [HttpGet("pagados-hoy")]
        public async Task<IActionResult> GetPagadosHoy([FromQuery] int idEmpresa)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            var data = await _service.GetPagadosDelDiaAsync(idEmpresa);
            return Ok(data);
        }

        [HttpGet("print")]
        public async Task<IActionResult> GetEncargoPrint(
            [FromQuery] int idFacturaHeader,
            [FromQuery] int idEmpresa)
        {
            if (idFacturaHeader <= 0 || idEmpresa <= 0)
                return BadRequest(new { message = "idFacturaHeader e idEmpresa son obligatorios" });

            try
            {
                var data = await _service.GetEncargoPrintByIdAsync(idFacturaHeader, idEmpresa);
                if (data == null)
                    return NotFound(new { message = "Encargo no encontrado" });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return MapException(ex);
            }
        }

        [HttpGet("facturas")]
        public async Task<IActionResult> GetFacturas(
            [FromQuery] int idEmpresa,
            [FromQuery] DateTime desde,
            [FromQuery] DateTime hasta)
        {
            if (idEmpresa <= 0)
                return BadRequest(new { message = "idEmpresa es obligatorio" });

            var data = await _service.GetFacturasPagadasAsync(idEmpresa, desde, hasta);
            return Ok(data);
        }

        private static IActionResult MapException(Exception ex)
        {
            return ex switch
            {
                ArgumentException => new BadRequestObjectResult(new { message = ex.Message }),
                KeyNotFoundException => new NotFoundObjectResult(new { message = ex.Message }),
                _ => new ObjectResult(new { message = "Error interno", detail = ex.Message })
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                }
            };
        }
        [HttpGet]
        [Route("GetCierreDelDia")]
        public async Task<IActionResult> GetCierreDelDia(int idEmpresa)
        {
            try
            {
                var result = await _service.GetCierreDelDiaAsync(idEmpresa);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error generando cierre de encargos",
                    error = ex.Message
                });
            }
        }
    }
}
