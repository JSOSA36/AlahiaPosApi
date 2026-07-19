using AlahiaPos.Entities.Events;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Publicador de eventos del ERP Core. No depende de Contabilidad.
    /// Los errores en consumidores nunca deben propagarse al llamador.
    /// </summary>
    public interface IDomainEventPublisher
    {
        Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : DomainEventBase;
    }
}
