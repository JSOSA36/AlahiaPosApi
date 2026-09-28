using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AlahiaPos.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngresosController : ControllerBase
    {
        private readonly IIngresos _ingresosService;
        private readonly
       IMetodoPagoCuentaService
       _MetodoPagoCuentaService;

        private readonly
        IMovimientoFinancieroService
        _MovimientoFinancieroService;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly ICuentaFinancieraService _cuentaFinancieraService;
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

        public IngresosController(IIngresos ingresosService, IMetodoPagoCuentaService
            metodoPagoCuentaService,

            IMovimientoFinancieroService
            movimientoFinancieroService,
            IContabilidadEventPublisher contabilidadEvents,
            ICuentaFinancieraService cuentaFinancieraService,
            ISucursalService sucursales,
            ISesionTokenResolver tokens)
        {
            _ingresosService = ingresosService;
            _MetodoPagoCuentaService =
                metodoPagoCuentaService;

            _MovimientoFinancieroService =
                movimientoFinancieroService;
            _contabilidadEvents = contabilidadEvents;
            _cuentaFinancieraService = cuentaFinancieraService;
            _sucursales = sucursales;
            _tokens = tokens;
        }

        // ================================================
        // 🔹 CRUD BÁSICO
        // ================================================

        // ✅ GET: api/Ingresos/GetAllIngresos/{IdEmpresa}
        [HttpGet("GetAllIngresos/{IdEmpresa}")]
        public async Task<IActionResult> GetAllIngresos(int IdEmpresa)
        {
            var result = await _ingresosService.GetAllIngresos(IdEmpresa);
            return Ok(result);
        }

        // ✅ GET: api/Ingresos/GetIngresoById/{IdIngreso}
        [HttpGet("GetIngresoById/{IdIngreso}")]
        public async Task<IActionResult> GetIngresoById(int IdIngreso)
        {
            var ingreso = await _ingresosService.GetIngresoById(IdIngreso);
            if (ingreso == null)
                return NotFound(new { message = $"No se encontró el ingreso con ID {IdIngreso}" });

            return Ok(ingreso);
        }

        // ✅ POST: api/Ingresos
        [HttpPost]
        public async Task<IActionResult>
 InsertIngreso(
     [FromBody]
    Ingresos ingreso
 )
        {
            try
            {
                if (ingreso == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "El objeto ingreso no puede ser nulo."
                    });
                }

                // Cobros ligados a factura: flujo histórico (sin IngresoExtraRegistrado).
                if (ingreso.IdFacturaHeader is > 0)
                {
                    ingreso.FechaRegistro = DateTime.Now;
                    await _ingresosService.InsertIngreso(ingreso);

                    var metodoCuentaFactura = await _MetodoPagoCuentaService
                        .GetByMetodoAsync(ingreso.IdEmpresa, ingreso.FormaPago);

                    if (metodoCuentaFactura != null && metodoCuentaFactura.IdCuentaFinanciera > 0)
                    {
                        await _MovimientoFinancieroService.RegistrarEntradaAsync(
                            ingreso.IdEmpresa,
                            ingreso.IdUsuario ?? 0,
                            metodoCuentaFactura.IdCuentaFinanciera,
                            ingreso.Monto,
                            $"Ingreso - {ingreso.Categoria}",
                            ingreso.Descripcion ?? "Entrada automática por ingreso",
                            categoria: "INGRESO",
                            referenciaId: ingreso.IdIngreso > 0 ? ingreso.IdIngreso : null,
                            referenciaTipo: "INGRESO");
                    }

                    return Ok(new
                    {
                        message = "Ingreso registrado correctamente.",
                        idIngreso = ingreso.IdIngreso
                    });
                }

                var result = await _ingresosService.RegistrarIngresoExtraCompletoAsync(
                    new RegistrarIngresoExtraRequest
                    {
                        IdEmpresa = ingreso.IdEmpresa,
                        IdUsuario = ingreso.IdUsuario ?? 0,
                        Monto = ingreso.Monto,
                        Descripcion = ingreso.Descripcion,
                        Categoria = ingreso.Categoria,
                        Origen = ingreso.Origen,
                        FormaPago = ingreso.FormaPago,
                        Referencia = ingreso.Referencia,
                        Nota = ingreso.Nota,
                        Fecha = DateTime.Now,
                        DesdeExtractoBancario = false
                    });

                return Ok(new
                {
                    message = "Ingreso registrado correctamente.",
                    idIngreso = result.IdIngreso,
                    contabilidadAdvertencia = result.ContabilidadAdvertencia
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ PUT: api/Ingresos/{IdIngreso}
        [HttpPut("{IdIngreso}")]
        public async Task<IActionResult> UpdateIngreso(int IdIngreso, [FromBody] Ingresos ingreso)
        {
            if (ingreso == null)
                return BadRequest(new { message = "El objeto ingreso no puede ser nulo." });

            await _ingresosService.UpdateIngreso(IdIngreso, ingreso);
            return Ok(new { message = "Ingreso actualizado correctamente ✅" });
        }

        // ✅ DELETE: api/Ingresos/{IdIngreso}
        [HttpDelete("{IdIngreso}")]
        public async Task<IActionResult> DeleteIngreso(int IdIngreso)
        {
            var ingreso = await _ingresosService.GetIngresoById(IdIngreso);
            if (ingreso == null)
                return NotFound(new { message = $"No se encontró el ingreso con ID {IdIngreso}" });

            var esManual = !ingreso.IdFacturaHeader.HasValue || ingreso.IdFacturaHeader.Value <= 0;

            // Ingresos generados por factura se revierten desde la anulación de la factura.
            if (!esManual)
            {
                _ingresosService.DeleteIngreso(IdIngreso);
                return Ok(new { message = "Ingreso eliminado correctamente ✅" });
            }

            if (ingreso.EstaAnulado)
                return BadRequest(new { message = "El ingreso ya está anulado." });

            // 🔴 revertir tesorería (reverso de la entrada por ingreso)
            try
            {
                var metodoCuenta = await _MetodoPagoCuentaService
                    .GetByMetodoAsync(ingreso.IdEmpresa, ingreso.FormaPago);

                if (metodoCuenta != null
                    && metodoCuenta.IdCuentaFinanciera > 0
                    && ingreso.Monto > 0)
                {
                    await _MovimientoFinancieroService.RegistrarSalidaAsync(
                        ingreso.IdEmpresa,
                        ingreso.IdUsuario ?? 0,
                        metodoCuenta.IdCuentaFinanciera,
                        ingreso.Monto,
                        $"Anulación ingreso - {ingreso.Categoria}",
                        "Reverso automático por anulación de ingreso",
                        categoria: "INGRESO",
                        referenciaId: ingreso.IdIngreso,
                        referenciaTipo: "INGRESO_ANULACION",
                        claveIdempotencia: $"INGRESO-ANUL-{ingreso.IdIngreso}");
                }
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            // Contabilidad: reverso del asiento de ingreso (no-op si Contabilidad apagada)
            try
            {
                await _contabilidadEvents.TryPublishAsync(new AlahiaPos.Entities.Events.IngresoExtraAnuladoEvent
                {
                    IdEmpresa = ingreso.IdEmpresa,
                    IdUsuario = ingreso.IdUsuario ?? 0,
                    Fecha = DateTime.Now,
                    ReferenciaId = ingreso.IdIngreso,
                    ReferenciaTipo = "Ingreso",
                    Motivo = "Anulación de ingreso"
                });
            }
            catch
            {
                // Nunca tumbar anulación operativa
            }

            ingreso.EstaAnulado = true;
            await _ingresosService.UpdateIngreso(ingreso.IdIngreso, ingreso);

            return Ok(new { message = "Ingreso anulado correctamente ✅" });
        }
        [HttpGet("ingresos-por-linea")]
        public async Task<IActionResult>
GetIngresosPorLinea(

    int idEmpresa,

    DateTime fechaInicio,

    DateTime fechaFin
)
        {
            try
            {

                // =========================================
                // 🔥 DATA
                // =========================================

                var data =
                    await _ingresosService
                    .GetIngresosPorLineaNegocio(

                        idEmpresa,

                        fechaInicio,

                        fechaFin
                    );

                // =========================================
                // 🔥 RESPONSE
                // =========================================

                return Ok(new
                {
                    success = true,

                    message =
                        "Ingresos obtenidos correctamente",

                    data = data
                });
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    success = false,

                    message =
                        ex.Message
                });
            }
        }
        // ✅ GET: api/Ingresos/GetIngresosByFecha/{IdEmpresa}/{fechaInicio}/{fechaFin}
        [HttpGet("GetIngresosByFecha/{IdEmpresa}/{fechaInicio}/{fechaFin}")]
        public async Task<IActionResult> GetIngresosByFecha(
            int IdEmpresa,
            DateTime fechaInicio,
            DateTime fechaFin,
            int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var result = await _ingresosService.GetIngresosByFecha(IdEmpresa, fechaInicio, fechaFin);
            result = (result ?? Enumerable.Empty<Ingresos>())
                .Where(i => scope.Incluye(i.IdSucursal))
                .ToList();
            return Ok(result);
        }
        [HttpGet("GetIngresosByFechaCaja/{IdEmpresa}/{fechaInicio}/{fechaFin}")]
        public async Task<IActionResult> GetIngresosByFechaCaja(
        int IdEmpresa,
        DateTime fechaInicio,
        DateTime fechaFin)
        {
            var result = await _ingresosService
                .GetIngresosByFechaCaja(IdEmpresa, fechaInicio, fechaFin);

            return Ok(result);
        }

        // ================================================
        // 🔹 MÉTODOS ESPECÍFICOS PARA DASHBOARD
        // ================================================

        // ✅ GET: api/Ingresos/TotalDia/{IdEmpresa}
        [HttpGet("GetTotalDia/{IdEmpresa}")]
        public async Task<IActionResult> GetTotalDia(int IdEmpresa)
        {
            var total = await _ingresosService.GetTotalIngresosDia(IdEmpresa);
            return Ok(total);
        }

        // ✅ GET: api/Ingresos/TotalMes/{IdEmpresa}
        [HttpGet("GetTotalMes/{IdEmpresa}")]
        public async Task<IActionResult> GetTotalMes(int IdEmpresa)
        {
            var total = await _ingresosService.GetTotalIngresosMes(IdEmpresa);
            return Ok(total);
        }

        // ✅ GET: api/Ingresos/Historico/{IdEmpresa}
        [HttpGet("GetHistorico/{IdEmpresa}")]
        public async Task<IActionResult> GetHistorico(int IdEmpresa)
        {
            var historico = await _ingresosService.GetHistoricoIngresos(IdEmpresa);
            return Ok(historico);
        }
    }
}
