using AlahiaPos.DataAccess.Servicios;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
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
        public IngresosController(IIngresos ingresosService, IMetodoPagoCuentaService
            metodoPagoCuentaService,

            IMovimientoFinancieroService
            movimientoFinancieroService)
        {
            _ingresosService = ingresosService;
            _MetodoPagoCuentaService =
                metodoPagoCuentaService;

            _MovimientoFinancieroService =
                movimientoFinancieroService;
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

                // =========================================
                // 🔥 VALIDAR
                // =========================================

                if (ingreso == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "El objeto ingreso no puede ser nulo."
                    });
                }

                // =========================================
                // 🔥 FECHA
                // =========================================

                ingreso.FechaRegistro =
                    DateTime.Now;

                // =========================================
                // 🔥 GUARDAR INGRESO
                // =========================================

                await _ingresosService
                .InsertIngreso(
                    ingreso
                );

                // =========================================
                // 🔥 MÉTODO CONFIGURADO
                // =========================================

                var metodoCuenta =
                    await _MetodoPagoCuentaService
                    .GetByMetodoAsync(

                        ingreso.IdEmpresa,

                        ingreso.FormaPago
                    );

                // =========================================
                // 🔥 REGISTRAR MOVIMIENTO
                // =========================================

                if (
                    metodoCuenta != null
                    &&
                    metodoCuenta
                    .IdCuentaFinanciera > 0
                )
                {

                    await _MovimientoFinancieroService
                    .RegistrarEntradaAsync(

                        ingreso.IdEmpresa,

                        ingreso.IdUsuario ?? 0,

                        metodoCuenta
                        .IdCuentaFinanciera,

                        ingreso.Monto,

                        $"Ingreso - {ingreso.Categoria}",

                        ingreso.Descripcion
                        ??
                        "Entrada automática por ingreso",

                        categoria: "INGRESO",

                        referenciaId: ingreso.IdIngreso > 0 ? ingreso.IdIngreso : null,

                        referenciaTipo: "INGRESO"
                    );
                }

                return Ok(new
                {
                    message =
                        "Ingreso registrado correctamente ✅"
                });
            }

            catch (Exception ex)
            {

                return BadRequest(new
                {
                    message =
                        ex.Message
                });
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
        public IActionResult DeleteIngreso(int IdIngreso)
        {
            _ingresosService.DeleteIngreso(IdIngreso);
            return Ok(new { message = "Ingreso eliminado correctamente ✅" });
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
        public async Task<IActionResult> GetIngresosByFecha(int IdEmpresa, DateTime fechaInicio, DateTime fechaFin)
        {
            var result = await _ingresosService.GetIngresosByFecha(IdEmpresa, fechaInicio, fechaFin);
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
