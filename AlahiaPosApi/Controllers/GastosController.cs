using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
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

                if (string.IsNullOrWhiteSpace(value.TipoGasto))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe seleccionar un tipo de gasto."
                    });
                }

                if (value.Monto <= 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe ingresar un monto válido."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.FormaPago))
                {
                    value.FormaPago = "EFECTIVO";
                }

                if (string.IsNullOrWhiteSpace(value.Orien))
                {
                    value.Orien = value.FormaPago;
                }

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
                        "Salida automática por gasto",

                        categoria: "GASTO",

                        referenciaTipo: "GASTO"
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
        public async Task<IActionResult> Put(
            int id,
            [FromBody] Gastos value
        )
        {
            try
            {
                var gastoExistente =
                    await _IGastos.GetGastosById(id);

                if (gastoExistente == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "El gasto no existe."
                    });
                }

                if (gastoExistente.EstaAnulado)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No se puede editar un gasto anulado."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.TipoGasto))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe seleccionar un tipo de gasto."
                    });
                }

                if (value.Monto <= 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe ingresar un monto válido."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.FormaPago))
                {
                    value.FormaPago = gastoExistente.FormaPago ?? "EFECTIVO";
                }

                if (string.IsNullOrWhiteSpace(value.Orien))
                {
                    value.Orien = value.FormaPago;
                }

                gastoExistente.IdGasto = id;
                gastoExistente.TipoGasto = value.TipoGasto;
                gastoExistente.Monto = value.Monto;
                gastoExistente.Orien = value.Orien;
                gastoExistente.Detalle = value.Detalle;
                gastoExistente.FormaPago = value.FormaPago;
                gastoExistente.Referencia = value.Referencia;
                gastoExistente.IdEmpleado = value.IdEmpleado;
                gastoExistente.IdUsuario = value.IdUsuario;
                gastoExistente.IdCuentaFinanciera = value.IdCuentaFinanciera;

                _IGastos.UpdateGastos(gastoExistente);

                return Ok(new
                {
                    success = true,
                    message = "Gasto actualizado correctamente."
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
        // 🔥 ANULAR
        // ======================================================

        [HttpPost]
        [Route("AnularGasto")]
        public async Task<IActionResult> AnularGasto(
            [FromBody] AnularGastoDto dto
        )
        {
            try
            {
                if (dto == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Datos de anulación requeridos."
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.MotivoAnulacion))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe indicar el motivo de anulación."
                    });
                }

                var motivo = dto.MotivoAnulacion.Trim();

                if (!string.IsNullOrWhiteSpace(dto.UsuarioAnulo))
                {
                    motivo = $"[{dto.UsuarioAnulo.Trim()}] {motivo}";
                }

                await _IGastos.AnularGastoAsync(
                    dto.IdGasto,
                    dto.IdEmpresa,
                    motivo,
                    dto.UsuarioAnulo);

                return Ok(new
                {
                    success = true,
                    message = "Gasto anulado correctamente."
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