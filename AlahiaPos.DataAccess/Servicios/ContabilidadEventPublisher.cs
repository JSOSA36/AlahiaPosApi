using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Puerta segura: Contabilidad OFF → no publica.
    /// Contabilidad ON → outbox + motor. Errores/mapeo faltante → Advertencia, nunca excepción al caller.
    /// </summary>
    public class ContabilidadEventPublisher : IContabilidadEventPublisher
    {
        private readonly IContabilidadGatekeeper _gatekeeper;
        private readonly IDomainEventPublisher _publisher;
        private readonly ContabilidadOperacionContext _contexto;

        public ContabilidadEventPublisher(
            IContabilidadGatekeeper gatekeeper,
            IDomainEventPublisher publisher,
            ContabilidadOperacionContext contexto)
        {
            _gatekeeper = gatekeeper;
            _publisher = publisher;
            _contexto = contexto;
        }

        public async Task<ContabilidadPublishResult> TryPublishAsync<TEvent>(TEvent domainEvent)
            where TEvent : DomainEventBase
        {
            try
            {
                var estado = await _gatekeeper.ObtenerEstadoAsync(domainEvent.IdEmpresa);
                if (!estado.DebeProcesarEventos)
                    return ContabilidadPublishResult.Inactiva();

                _contexto.Reset();
                await _publisher.PublishAsync(domainEvent);

                if (_contexto.HuboErrorOMapeoFaltante || !string.IsNullOrWhiteSpace(_contexto.UltimaAdvertencia))
                {
                    return ContabilidadPublishResult.ConAdvertencia(
                        _contexto.UltimaAdvertencia
                        ?? "No se pudo generar el asiento contable. Revise la Configuración de Integración.");
                }

                if (_contexto.AsientoGenerado)
                    return ContabilidadPublishResult.Ok();

                return ContabilidadPublishResult.ConAdvertencia(
                    "La operación se guardó, pero no se generó asiento contable. Revise el log de integración.");
            }
            catch
            {
                return ContabilidadPublishResult.ConAdvertencia(
                    "La operación se guardó, pero Contabilidad no pudo procesar el evento.");
            }
        }
    }
}
