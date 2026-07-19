using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Consumidor opcional de eventos del ERP. Solo actúa si el módulo está contratado
    /// y la integración automática está habilitada por empresa.
    /// </summary>
    public class ContabilidadEventHandler : IDomainEventHandler
    {
        private readonly IContabilidadGatekeeper _gatekeeper;
        private readonly IContabilidadIntegracionService _integracion;
        private readonly IContabilidadIntegracionLogService _log;
        private readonly IContabilidadConfiguracionService _configuracion;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ContabilidadEventHandler(
            IContabilidadGatekeeper gatekeeper,
            IContabilidadIntegracionService integracion,
            IContabilidadIntegracionLogService log,
            IContabilidadConfiguracionService configuracion)
        {
            _gatekeeper = gatekeeper;
            _integracion = integracion;
            _log = log;
            _configuracion = configuracion;
        }

        public async Task HandleAsync(EventoOutbox evento)
        {
            // Sprint B.1: eventos fiscales / comerciales genéricos sin asiento contable
            // → early return silencioso (sin gatekeeper ni log Omitido).
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
                switch (evento.TipoEvento)
                {
                    case DomainEventTypes.VentaConfirmada:
                        await ProcesarVentaConfirmadaAsync(evento);
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
                await _log.RegistrarAsync(
                    evento.IdEmpresa,
                    ContabilidadIntegracionEstados.Error,
                    ex.Message,
                    evento.IdEventoOutbox);
            }
        }

        private async Task ProcesarVentaConfirmadaAsync(EventoOutbox evento)
        {
            var venta = JsonSerializer.Deserialize<VentaConfirmadaEvent>(evento.Payload, JsonOptions);
            if (venta == null)
                throw new InvalidOperationException("No se pudo deserializar VentaConfirmadaEvent.");

            var config = await _configuracion.EnsureConfiguracionAsync(venta.IdEmpresa);
            var requests = ContabilidadVentaAsientoBuilder.Construir(venta, config);

            if (requests.Count == 0)
            {
                await _log.RegistrarAsync(
                    venta.IdEmpresa,
                    ContabilidadIntegracionEstados.Omitido,
                    "No se generaron líneas contables. Verifique el mapeo de cuentas.",
                    evento.IdEventoOutbox,
                    ContabilidadConstantes.OrigenVentas,
                    venta.ReferenciaId);
                return;
            }

            foreach (var request in requests)
            {
                var idAsiento = await _integracion.RegistrarAsientoAutomaticoAsync(request);
                await _log.RegistrarAsync(
                    venta.IdEmpresa,
                    ContabilidadIntegracionEstados.Ok,
                    $"Asiento generado: {request.TipoOperacion}",
                    evento.IdEventoOutbox,
                    request.OrigenModulo,
                    request.OrigenReferenciaId,
                    request.TipoOperacion,
                    idAsiento);
            }
        }
    }
}
