using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Única puerta de los módulos operativos hacia Contabilidad.
    /// - Contabilidad OFF → no-op (sin outbox, sin costo relevante).
    /// - Contabilidad ON → publica evento al motor central.
    /// Nunca lanza: el ERP operativo no depende de Contabilidad.
    /// </summary>
    public interface IContabilidadEventPublisher
    {
        Task<ContabilidadPublishResult> TryPublishAsync<TEvent>(TEvent domainEvent)
            where TEvent : DomainEventBase;
    }
}
