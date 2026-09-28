using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using AlahiaPosApi.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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
        private readonly ISucursalService _sucursales;
        private readonly ISesionTokenResolver _tokens;

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
            ICategoriaGastoService categoriaGastoService,
            ISucursalService sucursales,
            ISesionTokenResolver tokens
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
            _sucursales = sucursales;
            _tokens = tokens;
        }

        // ======================================================
        // 🔥 GET
        // ======================================================

        [HttpGet("{IdEmpresa}")]
        public async Task<IActionResult> Get(int IdEmpresa, int? idSucursalFiltro = null)
        {
            var (scope, error) = await SucursalConsultaHttp.ResolverAsync(
                HttpContext, _tokens, _sucursales, IdEmpresa, idSucursalFiltro);
            if (error != null)
                return error;

            var gastos = await _IGastos.GetAllGastos(IdEmpresa);
            gastos = (gastos ?? Enumerable.Empty<Gastos>())
                .Where(g => scope.Incluye(g.IdSucursal))
                .ToList();
            return Ok(gastos);
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

                if (GastoComprobanteTipos.EsGastosMenores(value.TipoComprobante)
                    && (value.IdTipoBienesServicios is null or < 1 or > 11))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Seleccione el Tipo de gasto DGII (606). Es obligatorio con comprobante de gastos menores."
                    });
                }

                if (GastoComprobanteTipos.EsGastosMenores(value.TipoComprobante))
                {
                    var reserva = await _secuenciaEcf.ReservarSiguienteAsync(
                        value.IdEmpresa,
                        GastoComprobanteTipos.TipoEcfGastosMenores,
                        value.IdSucursal);

                    if (reserva.Exitoso && !string.IsNullOrWhiteSpace(reserva.Encf))
                    {
                        value.NumeroComprobante = reserva.Encf;
                        value.FechaComprobante = DateTime.Today;
                    }
                    else
                    {
                        return Ok(new
                        {
                            success = false,
                            message = reserva.MensajeError
                                ?? "No hay secuencia E43 (Gastos Menores). Configure FE → Secuencias e-CF para que el gasto entre al Formato 606."
                        });
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
                    IdTipoBienesServicios = value.IdTipoBienesServicios,
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

                if (GastoComprobanteTipos.EsGastosMenores(gastoExistente.TipoComprobante))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Un gasto con comprobante de gastos menores no se puede editar (e-NCF E43 / Formato 606)."
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

                if (GastoComprobanteTipos.EsGastosMenores(value.TipoComprobante)
                    && (value.IdTipoBienesServicios is null or < 1 or > 11))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Seleccione el Tipo de gasto DGII (606). Es obligatorio con comprobante de gastos menores."
                    });
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
                            GastoComprobanteTipos.TipoEcfGastosMenores,
                            value.IdSucursal ?? gastoExistente.IdSucursal);

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
                gastoExistente.IdTipoBienesServicios = value.IdTipoBienesServicios is >= 1 and <= 11
                    ? value.IdTipoBienesServicios
                    : null;
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