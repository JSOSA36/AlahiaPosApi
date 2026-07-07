using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AlahiaPosApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GastosController : ControllerBase
    {

        // ======================================================
        // 🔥 SERVICES
        // ======================================================

        private readonly
        IGastos _IGastos;

        private readonly
        IMovimientoFinancieroService
        _MovimientoFinancieroService;

        private readonly
        IMetodoPagoCuentaService
        _MetodoPagoCuentaService;

        private readonly
        ICuentaFinancieraService
        _CuentaFinancieraService;

        // ======================================================
        // 🔥 CONSTRUCTOR
        // ======================================================

        public GastosController(

            IGastos iGastos,

            IMovimientoFinancieroService
            movimientoFinancieroService,

            IMetodoPagoCuentaService
            metodoPagoCuentaService,

            ICuentaFinancieraService
            cuentaFinancieraService
        )
        {
            _IGastos =
                iGastos;

            _MovimientoFinancieroService =
                movimientoFinancieroService;

            _MetodoPagoCuentaService =
                metodoPagoCuentaService;

            _CuentaFinancieraService =
                cuentaFinancieraService;
        }

        // ======================================================
        // 🔥 GET
        // ======================================================

        [HttpGet("{IdEmpresa}")]
        public async Task<
            IEnumerable<Gastos>>
            Get(int IdEmpresa)
        {
            return await _IGastos
            .GetAllGastos(
                IdEmpresa
            );
        }

        // ======================================================
        // 🔥 POST
        // ======================================================

        [HttpPost]
        public async Task<IActionResult> Post(
      [FromBody] Gastos value
  )
        {
            try
            {
                // =========================================
                // 🔥 DATOS AUTOMÁTICOS
                // =========================================

                value.FechaInseccion = DateTime.Now;
                value.IdProveedor = 1;

                // =========================================
                // 🔥 OBTENER CUENTA FINANCIERA
                // =========================================

                if (!value.IdCuentaFinanciera.HasValue)
                {
                    var metodoCuenta =
                        await _MetodoPagoCuentaService
                        .GetByMetodoAsync(

                            value.IdEmpresa,

                            value.FormaPago
                        );

                    if (metodoCuenta == null)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = "El método de pago seleccionado no tiene una cuenta financiera configurada."
                        });
                    }

                    value.IdCuentaFinanciera =
                        metodoCuenta.IdCuentaFinanciera;
                }

                // =========================================
                // 🔥 VALIDAR QUE EXISTA LA CUENTA
                // =========================================

                var cuenta =
                    await _CuentaFinancieraService
                    .GetByIdAsync(

                        value.IdCuentaFinanciera.Value
                    );

                if (cuenta == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "La cuenta financiera no existe."
                    });
                }

                // =========================================
                // 🔥 VALIDAR SALDO DISPONIBLE
                // =========================================

                if (cuenta.SaldoDisponible< value.Monto)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Fondos insuficientes."
                    });
                }

                // =========================================
                // 🔥 REGISTRAR SALIDA
                // =========================================

                await _MovimientoFinancieroService
                    .RegistrarSalidaAsync(

                        value.IdEmpresa,

                        value.IdEmpleado ?? 0,

                        cuenta.IdCuentaFinanciera,

                        value.Monto,

                        $"Gasto - {value.TipoGasto}",

                        value.Detalle ??
                        "Salida automática por gasto"
                    );

                // =========================================
                // 🔥 GUARDAR GASTO
                // =========================================

                await _IGastos.InsertGastos(value);

                // =========================================
                // 🔥 RESPUESTA
                // =========================================

                return Ok(new
                {
                    success = true,
                    message = "Gasto registrado correctamente."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ======================================================
        // 🔥 PUT
        // ======================================================

        [HttpPut("{id}")]
        public void Put(
            int id,

            [FromBody]
            string value
        )
        {

        }

        // ======================================================
        // 🔥 DELETE
        // ======================================================

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _IGastos
            .DeleteGastos(id);
        }

        // ======================================================
        // 🔥 TOTAL MES
        // ======================================================

        [HttpGet]
        [Route("GetTotalGastos")]
        public async Task<decimal>
        GetTotalGastos(
            int IdEmpresa
        )
        {
            return await _IGastos
            .TotalGastosDelMes(
                IdEmpresa
            );
        }
    }
}