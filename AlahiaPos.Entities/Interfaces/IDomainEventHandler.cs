using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    public interface IDomainEventHandler
    {
        Task HandleAsync(EventoOutbox evento);
    }
}
