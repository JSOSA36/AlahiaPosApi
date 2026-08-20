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

        private readonly ISecuenciaEcfService _secuenciaEcf;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly ICategoriaGastoService _categoriaGastoService;

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
            cuentaFinancieraService,

            ISecuenciaEcfService secuenciaEcf,
            IContabilidadEventPublisher contabilidadEvents,
            ICategoriaGastoService categoriaGastoService
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

            _secuenciaEcf = secuenciaEcf;
            _contabilidadEvents = contabilidadEvents;
            _categoriaGastoService = categoriaGastoService;
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
                if (string.IsNullOrWhiteSpace(value.TipoGasto))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe seleccionar una categoría de gasto."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.TipoComprobante))
                {
                    value.TipoComprobante = GastoComprobanteTipos.SinComprobante;
                }

                if (GastoComprobanteTipos.EsGastosMenores(value.TipoComprobante))
                {
                    var reserva = await _secuenciaEcf.ReservarSiguienteAsync(
                        value.IdEmpresa,
                        GastoComprobanteTipos.TipoEcfGastosMenores);

                    if (reserva.Exitoso && !string.IsNullOrWhiteSpace(reserva.Encf))
                    {
                        value.NumeroComprobante = reserva.Encf;
                        value.FechaComprobante = DateTime.Today;
                    }
                    else
                    {
                        value.NumeroComprobante = null;
                        value.FechaComprobante = null;
                    }

                    value.RncEmisorComprobante = null;
                    value.NombreEmisorComprobante = null;
                }
                else
                {
                    value.NumeroComprobante = null;
                    value.FechaComprobante = null;
                    value.RncEmisorComprobante = null;
                    value.NombreEmisorComprobante = null;
                }

                if (value.Monto <= 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe ingresar un monto válido."
                    });
                }

                var result = await _IGastos.RegistrarGastoCompletoAsync(new RegistrarGastoRequest
                {
                    IdEmpresa = value.IdEmpresa,
                    IdUsuario = value.IdUsuario ?? value.IdEmpleado ?? 0,
                    Monto = value.Monto,
                    TipoGasto = value.TipoGasto!,
                    IdCategoriaGasto = value.IdCategoriaGasto,
                    Detalle = value.Detalle,
                    FormaPago = value.FormaPago,
                    IdCuentaFinanciera = value.IdCuentaFinanciera,
                    Referencia = value.Referencia,
                    OrigenModulo = string.IsNullOrWhiteSpace(value.OrigenModulo) ? "MANUAL" : value.OrigenModulo,
                    Fecha = DateTime.Now,
                    TipoComprobante = value.TipoComprobante,
                    NumeroComprobante = value.NumeroComprobante,
                    FechaComprobante = value.FechaComprobante,
                    IdProveedor = value.IdProveedor > 0 ? value.IdProveedor : 1,
                    DesdeExtractoBancario = false
                });

                return Ok(new
                {
                    success = true,
                    message = "Gasto registrado correctamente.",
                    idGasto = result.IdGasto,
                    contabilidadAdvertencia = result.ContabilidadAdvertencia
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

                if (string.Equals(gastoExistente.OrigenModulo, "NOMINA", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(gastoExistente.OrigenModulo, "COMPRAS", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Este gasto se originó en otro módulo y no se edita desde aquí."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.TipoGasto))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Debe seleccionar una categoría de gasto."
                    });
                }

                if (string.IsNullOrWhiteSpace(value.TipoComprobante))
                {
                    value.TipoComprobante = GastoComprobanteTipos.SinComprobante;
                }

                if (GastoComprobanteTipos.EsGastosMenores(value.TipoComprobante))
                {
                    // Conservar e-NCF ya asignado; si no tiene, intentar reservar sin bloquear
                    if (!string.IsNullOrWhiteSpace(gastoExistente.NumeroComprobante)
                        && GastoComprobanteTipos.EsGastosMenores(gastoExistente.TipoComprobante))
                    {
                        value.NumeroComprobante = gastoExistente.NumeroComprobante;
                        value.FechaComprobante = gastoExistente.FechaComprobante ?? DateTime.Today;
                    }
                    else
                    {
                        var reserva = await _secuenciaEcf.ReservarSiguienteAsync(
                            gastoExistente.IdEmpresa,
                            GastoComprobanteTipos.TipoEcfGastosMenores);

                        if (reserva.Exitoso && !string.IsNullOrWhiteSpace(reserva.Encf))
                        {
                            value.NumeroComprobante = reserva.Encf;
                            value.FechaComprobante = DateTime.Today;
                        }
                        else
                        {
                            value.NumeroComprobante = null;
                            value.FechaComprobante = null;
                        }
                    }

                    value.RncEmisorComprobante = null;
                    value.NombreEmisorComprobante = null;
                }
                else
                {
                    value.NumeroComprobante = null;
                    value.FechaComprobante = null;
                    value.RncEmisorComprobante = null;
                    value.NombreEmisorComprobante = null;
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
                gastoExistente.IdCategoriaGasto = value.IdCategoriaGasto;
                gastoExistente.TipoComprobante = value.TipoComprobante;
                gastoExistente.NumeroComprobante = value.NumeroComprobante;
                gastoExistente.FechaComprobante = value.FechaComprobante;
                gastoExistente.RncEmisorComprobante = value.RncEmisorComprobante;
                gastoExistente.NombreEmisorComprobante = value.NombreEmisorComprobante;
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

                await _contabilidadEvents.TryPublishAsync(new AlahiaPos.Entities.Events.GastoAnuladoEvent
                {
                    IdEmpresa = dto.IdEmpresa,
                    IdUsuario = 0,
                    Fecha = DateTime.Now,
                    ReferenciaId = dto.IdGasto,
                    ReferenciaTipo = "Gasto",
                    Motivo = motivo
                });

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