using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlahiaPos.DataAccess.Servicios
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly AlahiaPosContext _context;
        private readonly IEnumerable<IDomainEventHandler> _handlers;

        public DomainEventDispatcher(
            AlahiaPosContext context,
            IEnumerable<IDomainEventHandler> handlers)
        {
            _context = context;
            _handlers = handlers;
        }

        public async Task DispatchAsync(EventoOutbox evento)
        {
            evento.Intentos++;
            var huboError = false;
            string? mensajeError = null;

            foreach (var handler in _handlers)
            {
                try
                {
                    await handler.HandleAsync(evento);
                }
                catch (Exception ex)
                {
                    huboError = true;
                    mensajeError = ex.Message;
                }
            }

            evento.FechaProcesado = DateTime.Now;
            evento.Estado = huboError ? EventoOutboxEstados.Error : EventoOutboxEstados.Procesado;
            evento.MensajeError = mensajeError;

            _context.EventosOutbox.Update(evento);
            await _context.SaveChangesAsync();
        }
    }
}
