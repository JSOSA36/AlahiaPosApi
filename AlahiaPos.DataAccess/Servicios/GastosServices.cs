using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class GastosServices : IGastos
    {
        private readonly IRepository<Gastos> _services;
        private readonly IMovimientoFinancieroService _movimientoFinancieroService;
        private readonly IMetodoPagoCuentaService _metodoPagoCuentaService;
        private readonly ICuentaFinancieraService _cuentaFinancieraService;
        private readonly IContabilidadEventPublisher _contabilidadEvents;
        private readonly ICategoriaGastoService _categoriaGastoService;
        private readonly ISecuenciaEcfService _secuenciaEcf;
        private readonly AlahiaPosContext _context;

        public GastosServices(
            IRepository<Gastos> services,
            IMovimientoFinancieroService movimientoFinancieroService,
            IMetodoPagoCuentaService metodoPagoCuentaService,
            ICuentaFinancieraService cuentaFinancieraService,
            IContabilidadEventPublisher contabilidadEvents,
            ICategoriaGastoService categoriaGastoService,
            ISecuenciaEcfService secuenciaEcf,
            AlahiaPosContext context)
        {
            _services = services;
            _movimientoFinancieroService = movimientoFinancieroService;
            _metodoPagoCuentaService = metodoPagoCuentaService;
            _cuentaFinancieraService = cuentaFinancieraService;
            _contabilidadEvents = contabilidadEvents;
            _categoriaGastoService = categoriaGastoService;
            _secuenciaEcf = secuenciaEcf;
            _context = context;
        }

        public async Task CerrarGastosPendientes(
            int idEmpresa,
            int idUsuario,
            int idCajaCierre)
        {
            var gastos = await _services.GetAllByExpresionAsync(x =>
                x.IdEmpresa == idEmpresa
                && x.IdUsuario == idUsuario
                && x.EstaAnulado == false
                && x.EstaCerrada != true
                && x.FormaPago != null
                && x.FormaPago.ToUpper() == "EFECTIVO");

            foreach (var gasto in gastos)
            {
                gasto.EstaCerrada = true;
                gasto.IdCajaCierre = idCajaCierre;
                _services.Update(gasto.IdGasto, gasto);
            }
        }

        public async Task AnularGastoAsync(
            int idGasto,
            int idEmpresa,
            string motivoAnulacion,
            string? usuarioAnulo)
        {
            var gasto = await _services.GetByExpresionAsync(x =>
                x.IdGasto == idGasto
                && x.IdEmpresa == idEmpresa);

            if (gasto == null)
                throw new KeyNotFoundException("El gasto no existe.");

            if (gasto.EstaAnulado)
                throw new InvalidOperationException("El gasto ya está anulado.");

            if (gasto.EstaCerrada == true)
                throw new InvalidOperationException(
                    "No se puede anular un gasto ya cerrado en caja.");

            if (string.Equals(gasto.OrigenModulo, "COMPRAS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(gasto.OrigenModulo, "NOMINA", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "Este gasto se originó en otro módulo. Anúlelo o reviértalo desde su origen.");

            var idCuentaFinanciera = gasto.IdCuentaFinanciera;

            if (!idCuentaFinanciera.HasValue || idCuentaFinanciera.Value <= 0)
            {
                var formaPago = string.IsNullOrWhiteSpace(gasto.FormaPago)
                    ? "EFECTIVO"
                    : gasto.FormaPago;

                var metodoCuenta = await _metodoPagoCuentaService
                    .GetByMetodoAsync(idEmpresa, formaPago);

                if (metodoCuenta == null)
                    throw new InvalidOperationException(
                        "No se encontró la cuenta financiera asociada al gasto.");

                idCuentaFinanciera = metodoCuenta.IdCuentaFinanciera;
            }

            await _movimientoFinancieroService.RegistrarEntradaAsync(
                idEmpresa,
                gasto.IdUsuario ?? gasto.IdEmpleado ?? 0,
                idCuentaFinanciera.Value,
                gasto.Monto,
                $"Anulación gasto - {gasto.TipoGasto}",
                motivoAnulacion,
                categoria: "GASTO",
                referenciaId: gasto.IdGasto,
                referenciaTipo: "GASTO_ANULACION",
                claveIdempotencia: $"GASTO-ANUL-{gasto.IdGasto}");

            gasto.EstaAnulado = true;
            gasto.MotivoAnulacion = motivoAnulacion;

            _services.Update(gasto.IdGasto, gasto);
        }

        public async Task<RegistrarGastoResult> RegistrarGastoCompletoAsync(RegistrarGastoRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.TipoGasto))
                throw new InvalidOperationException("Debe seleccionar una categoría de gasto.");

            if (request.Monto <= 0)
                throw new InvalidOperationException("Debe ingresar un monto válido.");

            // Idempotencia desde extracto: reutilizar gasto/movimiento ya creados.
            if (!string.IsNullOrWhiteSpace(request.ClaveIdempotencia))
            {
                var movExistente = await _context.MovimientoFinanciero.AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IdEmpresa == request.IdEmpresa
                        && x.ClaveIdempotencia == request.ClaveIdempotencia);

                if (movExistente != null)
                {
                    var gastoExistente = await _context.Gastos.AsNoTracking()
                        .FirstOrDefaultAsync(g =>
                            g.IdEmpresa == request.IdEmpresa
                            && g.Referencia == (request.Referencia ?? $"EXT-{request.IdTesoreriaExtractoLinea}")
                            && g.EstaAnulado == false);

                    return new RegistrarGastoResult
                    {
                        IdGasto = gastoExistente?.IdGasto ?? 0,
                        IdMovimientoFinanciero = movExistente.IdMovimientoFinanciero,
                        YaExistia = true
                    };
                }
            }

            var formaPago = string.IsNullOrWhiteSpace(request.FormaPago)
                ? (request.DesdeExtractoBancario ? "TRANSFERENCIA" : "EFECTIVO")
                : request.FormaPago.Trim();

            var idCuenta = request.IdCuentaFinanciera;
            if (!idCuenta.HasValue || idCuenta.Value <= 0)
            {
                var metodoCuenta = await _metodoPagoCuentaService.GetByMetodoAsync(
                    request.IdEmpresa, formaPago)
                    ?? throw new InvalidOperationException(
                        "El método de pago seleccionado no tiene una cuenta financiera configurada.");
                idCuenta = metodoCuenta.IdCuentaFinanciera;
            }

            var cuenta = await _cuentaFinancieraService.GetByIdAsync(idCuenta.Value)
                ?? throw new InvalidOperationException("La cuenta financiera no existe.");

            if (!request.DesdeExtractoBancario && cuenta.SaldoDisponible < request.Monto)
                throw new InvalidOperationException("Fondos insuficientes.");

            var fecha = request.Fecha ?? DateTime.Now;
            var tipoComprobante = string.IsNullOrWhiteSpace(request.TipoComprobante)
                ? GastoComprobanteTipos.SinComprobante
                : request.TipoComprobante;

            var origenModulo = string.IsNullOrWhiteSpace(request.OrigenModulo)
                ? "MANUAL"
                : request.OrigenModulo.Trim().ToUpperInvariant();

            var referencia = request.Referencia;
            if (string.IsNullOrWhiteSpace(referencia) && request.IdTesoreriaExtractoLinea is > 0)
                referencia = $"EXT-{request.IdTesoreriaExtractoLinea}";

            var numeroComprobante = string.IsNullOrWhiteSpace(request.NumeroComprobante)
                ? null
                : request.NumeroComprobante.Trim().ToUpperInvariant();
            DateTime? fechaComprobante = request.FechaComprobante;
            string? rncEmisor = null;
            string? nombreEmisor = null;
            string? mensajeEcf = null;

            // Gastos Menores: asignar e-NCF E43 si no viene número (requerido para Formato 606).
            if (GastoComprobanteTipos.EsGastosMenores(tipoComprobante)
                && string.IsNullOrWhiteSpace(numeroComprobante))
            {
                var reserva = await _secuenciaEcf.ReservarSiguienteAsync(
                    request.IdEmpresa,
                    GastoComprobanteTipos.TipoEcfGastosMenores);

                if (reserva.Exitoso && !string.IsNullOrWhiteSpace(reserva.Encf))
                {
                    numeroComprobante = reserva.Encf;
                    fechaComprobante ??= fecha.Date;
                    var empresa = await _context.Empresas.AsNoTracking()
                        .FirstOrDefaultAsync(e => e.IdEmpresa == request.IdEmpresa);
                    rncEmisor = empresa?.RNC;
                    nombreEmisor = empresa?.NombreComercial;
                }
                else
                {
                    mensajeEcf = reserva.MensajeError
                        ?? "No hay secuencia E43 (Gastos Menores). Configure FE → Secuencias e-CF.";
                }
            }

            // Movimiento primero (categoría GASTO → contabilidad por GastoRegistrado, no por Banco).
            var referenciaTipoMov = request.DesdeExtractoBancario ? "EXTRACTO" : "GASTO";
            var referenciaIdMov = request.DesdeExtractoBancario
                ? request.IdTesoreriaExtractoLinea
                : null;

            await _movimientoFinancieroService.RegistrarSalidaAsync(
                request.IdEmpresa,
                request.IdUsuario,
                cuenta.IdCuentaFinanciera,
                request.Monto,
                $"Gasto - {request.TipoGasto.Trim()}",
                request.Detalle ?? "Salida automática por gasto",
                categoria: "GASTO",
                referenciaId: referenciaIdMov,
                referenciaTipo: referenciaTipoMov,
                claveIdempotencia: request.ClaveIdempotencia);

            int? idMov = null;
            if (!string.IsNullOrWhiteSpace(request.ClaveIdempotencia))
            {
                var mov = await _context.MovimientoFinanciero.AsTracking()
                    .FirstOrDefaultAsync(x =>
                        x.IdEmpresa == request.IdEmpresa
                        && x.ClaveIdempotencia == request.ClaveIdempotencia);
                if (mov != null)
                {
                    idMov = mov.IdMovimientoFinanciero;
                    if (request.FechaMovimiento.HasValue)
                        mov.FechaMovimiento = request.FechaMovimiento.Value;
                    if (request.IdTesoreriaConciliacion is > 0)
                    {
                        mov.IdTesoreriaConciliacion = request.IdTesoreriaConciliacion;
                        mov.EstadoConciliacion = "CONCILIADO";
                        mov.FechaConciliacion = DateTime.UtcNow;
                        mov.IdUsuarioConciliacion = request.IdUsuario;
                    }
                }
            }

            var gasto = new Gastos
            {
                IdEmpresa = request.IdEmpresa,
                FechaInseccion = fecha,
                IdProveedor = request.IdProveedor > 0 ? request.IdProveedor : 1,
                TipoGasto = request.TipoGasto.Trim(),
                IdCategoriaGasto = request.IdCategoriaGasto,
                TipoComprobante = tipoComprobante,
                NumeroComprobante = numeroComprobante,
                FechaComprobante = fechaComprobante,
                RncEmisorComprobante = rncEmisor,
                NombreEmisorComprobante = nombreEmisor,
                Monto = request.Monto,
                Detalle = request.Detalle,
                IdUsuario = request.IdUsuario,
                IdEmpleado = request.IdUsuario,
                FormaPago = formaPago,
                Orien = formaPago,
                IdCuentaFinanciera = idCuenta,
                Referencia = referencia,
                OrigenModulo = origenModulo,
                EstaAnulado = false,
                EstaCerrada = false
            };

            await _services.Save(gasto);

            // Si el movimiento se creó sin clave, vincular por referencia tipo GASTO tras insert.
            if (idMov is not > 0 && !request.DesdeExtractoBancario)
            {
                // Controller clásico no devolvía idMov; se deja null.
            }

            int? idCuentaGastoCategoria = null;
            if (gasto.IdCategoriaGasto is > 0)
            {
                try
                {
                    var cat = await _categoriaGastoService.GetByIdAsync(gasto.IdCategoriaGasto.Value);
                    if (cat != null && cat.IdEmpresa == gasto.IdEmpresa && cat.IdCuentaContable is > 0)
                        idCuentaGastoCategoria = cat.IdCuentaContable;
                }
                catch
                {
                    /* fallback a mapeo GASTO_OPERATIVO */
                }
            }

            var contab = await _contabilidadEvents.TryPublishAsync(new GastoRegistradoEvent
            {
                IdEmpresa = gasto.IdEmpresa,
                IdUsuario = gasto.IdUsuario ?? gasto.IdEmpleado ?? 0,
                Fecha = gasto.FechaInseccion == default ? DateTime.Now : gasto.FechaInseccion,
                ReferenciaId = gasto.IdGasto,
                ReferenciaTipo = "Gasto",
                Monto = gasto.Monto,
                TipoGasto = gasto.TipoGasto,
                FormaPago = gasto.FormaPago,
                TipoCuentaFinanciera = cuenta.TipoCuenta,
                IdCuentaFinanciera = gasto.IdCuentaFinanciera,
                IdCategoriaGasto = gasto.IdCategoriaGasto,
                IdCuentaContableGasto = idCuentaGastoCategoria,
                Detalle = gasto.Detalle
            });

            return new RegistrarGastoResult
            {
                IdGasto = gasto.IdGasto,
                IdMovimientoFinanciero = idMov,
                ContabilidadAdvertencia = contab.Advertencia
                    ?? (string.IsNullOrWhiteSpace(mensajeEcf) ? null : mensajeEcf),
                YaExistia = false
            };
        }

        public Task<IEnumerable<Gastos>> GetAllGastos(int IdEmpresa)
        {
            return _services.GetAllByExpresionAsync(c => c.IdEmpresa == IdEmpresa);
        }

        public Task<Gastos> GetGastosById(int id)
        {
            return _services.GetByIdAsync(id);
        }

        public Task InsertGastos(Gastos gastos)
        {
            return _services.Save(gastos);
        }

        public async Task<decimal> TotalGastosDelMes(int IdEmpresa)
        {
            var result = await _services.GetAllByExpresionAsync(c =>
                c.FechaInseccion.Year == DateTime.Now.Year
                && c.FechaInseccion.Month == DateTime.Now.Month
                && c.IdEmpresa == IdEmpresa
                && c.EstaAnulado == false);

            return result?.Sum(c => c.Monto) ?? 0;
        }

        public void UpdateGastos(Gastos gastos)
        {
            _services.Update(gastos.IdGasto, gastos);
        }
    }
}
