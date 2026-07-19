using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDomainEventDispatcher
    {
        Task DispatchAsync(EventoOutbox evento);
    }
}
