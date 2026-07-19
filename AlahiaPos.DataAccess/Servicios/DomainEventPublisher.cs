using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios
{
    /// <summary>
    /// Publicador neutro de eventos. Persiste en outbox y despacha consumidores sin propagar errores.
    /// </summary>
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly AlahiaPosContext _context;
        private readonly IDomainEventDispatcher _dispatcher;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public DomainEventPublisher(
            AlahiaPosContext context,
            IDomainEventDispatcher dispatcher)
        {
            _context = context;
            _dispatcher = dispatcher;
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : DomainEventBase
        {
            var outbox = new EventoOutbox
            {
                IdEmpresa = domainEvent.IdEmpresa,
                TipoEvento = domainEvent.TipoEvento,
                ReferenciaId = domainEvent.ReferenciaId,
                ReferenciaTipo = domainEvent.ReferenciaTipo,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
                Estado = EventoOutboxEstados.Pendiente,
                FechaCreacion = DateTime.Now
            };

            _context.EventosOutbox.Add(outbox);
            await _context.SaveChangesAsync();

            try
            {
                await _dispatcher.DispatchAsync(outbox);
            }
            catch
            {
                // El outbox queda en Pendiente/Error. La operación de negocio no se afecta.
            }
        }
    }
}
