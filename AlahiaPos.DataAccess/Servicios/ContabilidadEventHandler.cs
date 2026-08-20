using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Consumidor opcional. Solo genera asientos si Contabilidad está contratada
    /// e IntegracionAutomatica activa. Si Contabilidad está apagada: no-op (log OMITIDO).
    /// </summary>
    public class ContabilidadEventHandler : IDomainEventHandler
    {
        private readonly IContabilidadGatekeeper _gatekeeper;
        private readonly IContabilidadIntegracionService _integracion;
        private readonly IContabilidadIntegracionLogService _log;
        private readonly IContabilidadConfiguracionService _configuracion;
        private readonly IContabilidadCuentaMapeoService _mapeo;
        private readonly ContabilidadOperacionContext _contexto;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ContabilidadEventHandler(
            IContabilidadGatekeeper gatekeeper,
            IContabilidadIntegracionService integracion,
            IContabilidadIntegracionLogService log,
            IContabilidadConfiguracionService configuracion,
            IContabilidadCuentaMapeoService mapeo,
            ContabilidadOperacionContext contexto)
        {
            _gatekeeper = gatekeeper;
            _integracion = integracion;
            _log = log;
            _configuracion = configuracion;
            _mapeo = mapeo;
            _contexto = contexto;
        }

        public async Task HandleAsync(EventoOutbox evento)
        {
            if (evento.TipoEvento == DomainEventTypes.DocumentoComercialConfirmado
                || evento.TipoEvento == DomainEventTypes.FiscalFotografiaPendiente
                || evento.TipoEvento == DomainEventTypes.ProduccionTrabajoSolicitado
                || evento.TipoEvento == DomainEventTypes.ProduccionTrabajoActualizado)
            {
                return;
            }

            var estado = await _gatekeeper.ObtenerEstadoAsync(evento.IdEmpresa);

            if (!estado.ModuloContratado)
            {
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Omitido,
                    "Módulo de Contabilidad no contratado.",
                    evento.IdEventoOutbox);
                return;
            }

            if (!estado.IntegracionAutomaticaActiva)
            {
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Omitido,
                    "Integración automática desactivada para esta empresa.",
                    evento.IdEventoOutbox);
                return;
            }

            try
            {
                await _mapeo.EnsureMapeoDefaultAsync(evento.IdEmpresa);

                switch (evento.TipoEvento)
                {
                    case DomainEventTypes.VentaConfirmada:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeVentaAsync(
                                Deserialize<VentaConfirmadaEvent>(evento),
                                await _configuracion.EnsureConfiguracionAsync(evento.IdEmpresa),
                                _mapeo));
                        break;

                    case DomainEventTypes.VentaAnulada:
                    {
                        var ev = Deserialize<VentaAnuladaEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenVentas,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.NotaCreditoCreada:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeNotaCreditoAsync(
                                Deserialize<NotaCreditoCreadaEvent>(evento),
                                await _configuracion.EnsureConfiguracionAsync(evento.IdEmpresa),
                                _mapeo));
                        break;

                    case DomainEventTypes.GastoRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeGastoAsync(
                                Deserialize<GastoRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.GastoAnulado:
                    {
                        var ev = Deserialize<GastoAnuladoEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenGastos,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.IngresoExtraRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeIngresoAsync(
                                Deserialize<IngresoExtraRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.IngresoExtraAnulado:
                    {
                        var ev = Deserialize<IngresoExtraAnuladoEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenIngresos,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.NotaCreditoAnulada:
                    {
                        var ev = Deserialize<NotaCreditoAnuladaEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenNotasCredito,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.CompraConfirmada:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeCompraAsync(
                                Deserialize<CompraConfirmadaEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.CompraAnulada:
                    {
                        var ev = Deserialize<CompraAnuladaEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenCompras,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.CobroClienteRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeCobroClienteAsync(
                                Deserialize<CobroClienteRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.PagoProveedorRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdePagoProveedorAsync(
                                Deserialize<PagoProveedorRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.MovimientoBancarioRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeMovimientoBancarioAsync(
                                Deserialize<MovimientoBancarioRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.InventarioMovimientoRegistrado:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeInventarioAsync(
                                Deserialize<InventarioMovimientoRegistradoEvent>(evento),
                                _mapeo));
                        break;

                    case DomainEventTypes.InventarioMovimientoAnulado:
                    {
                        var ev = Deserialize<InventarioMovimientoAnuladoEvent>(evento);
                        await RevertirOrigenAsync(
                            evento,
                            ContabilidadConstantes.OrigenInventario,
                            ev.ReferenciaId,
                            ev.IdUsuario,
                            ev.Motivo);
                        break;
                    }

                    case DomainEventTypes.NominaPagada:
                        await ProcesarRequestsAsync(
                            evento,
                            await ContabilidadAsientoBuilders.DesdeNominaPagadaAsync(
                                Deserialize<NominaPagadaEvent>(evento),
                                _mapeo));
                        break;

                    default:
                        await _log.RegistrarAsync(
                            evento.IdEmpresa,
                            ContabilidadIntegracionEstados.Omitido,
                            $"Procesador contable pendiente para evento '{evento.TipoEvento}'.",
                            evento.IdEventoOutbox);
                        break;
                }
            }
            catch (Exception ex)
            {
                var mensaje = ex.Message.Contains("mapeo", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("Falta mapeo", StringComparison.OrdinalIgnoreCase)
                    ? $"Falta configuración contable: {ex.Message}"
                    : ex.Message;

                _contexto.MarcarAdvertencia(mensaje);
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Error,
                    mensaje,
                    evento.IdEventoOutbox);
            }
        }

        private async Task ProcesarRequestsAsync(
            EventoOutbox evento,
            List<ContabilidadIntegracionRequest> requests)
        {
            if (requests.Count == 0)
            {
                const string msg = "No se generaron líneas contables. Verifique la Configuración de Integración Contable (mapeo de cuentas).";
                _contexto.MarcarAdvertencia(msg);
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Omitido,
                    msg,
                    evento.IdEventoOutbox);
                return;
            }

            foreach (var request in requests)
            {
                var idAsiento = await _integracion.RegistrarAsientoAutomaticoAsync(request);
                await _log.RegistrarAsync(
                    request.IdEmpresa,
                    ContabilidadIntegracionEstados.Ok,
                    $"Asiento generado: {request.TipoOperacion}",
                    evento.IdEventoOutbox,
                    request.OrigenModulo,
                    request.OrigenReferenciaId,
                    request.TipoOperacion,
                    idAsiento);
            }

            _contexto.MarcarOk();
        }

        private async Task RevertirOrigenAsync(
            EventoOutbox evento,
            string origenModulo,
            int origenReferenciaId,
            int idUsuario,
            string? motivo)
        {
            try
            {
                var ids = await _integracion.RevertirAsientosOrigenAsync(
                    evento.IdEmpresa,
                    origenModulo,
                    origenReferenciaId,
                    idUsuario > 0 ? idUsuario : 1,
                    motivo);

                foreach (var id in ids)
                {
                    await _log.RegistrarAsync(
                        evento.IdEmpresa,
                        ContabilidadIntegracionEstados.Ok,
                        "Asiento reverso generado.",
                        evento.IdEventoOutbox,
                        origenModulo,
                        origenReferenciaId,
                        ContabilidadTipoOperacion.Reverso,
                        id);
                }

                _contexto.MarcarOk();
            }
            catch (Exception ex)
            {
                _contexto.MarcarAdvertencia(ex.Message);
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Error,
                    ex.Message,
                    evento.IdEventoOutbox,
                    origenModulo,
                    origenReferenciaId);
            }
        }

        private static T Deserialize<T>(EventoOutbox evento)
        {
            var value = JsonSerializer.Deserialize<T>(evento.Payload, JsonOptions);
            if (value == null)
                throw new InvalidOperationException($"No se pudo deserializar {typeof(T).Name}.");
            return value;
        }
    }
}
