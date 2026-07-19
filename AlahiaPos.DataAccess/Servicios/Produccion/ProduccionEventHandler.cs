using System;
using System.Text.Json;
using System.Threading.Tasks;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Produccion
{
    /// <summary>
    /// Consume eventos de producción. No conoce módulos origen ni lee tablas comerciales.
    /// </summary>
    public class ProduccionEventHandler : IDomainEventHandler
    {
        private readonly IProduccionTrabajoService _trabajos;
        private readonly IProduccionRealtime _realtime;
        private readonly ILogger<ProduccionEventHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public ProduccionEventHandler(
            IProduccionTrabajoService trabajos,
            IProduccionRealtime realtime,
            ILogger<ProduccionEventHandler> logger)
        {
            _trabajos = trabajos;
            _realtime = realtime;
            _logger = logger;
        }

        public async Task HandleAsync(EventoOutbox evento)
        {
            if (evento.TipoEvento == DomainEventTypes.ProduccionTrabajoSolicitado)
            {
                await HandleSolicitadoAsync(evento);
                return;
            }

            if (evento.TipoEvento == DomainEventTypes.ProduccionTrabajoActualizado)
            {
                await HandleActualizadoAsync(evento);
            }
        }

        private async Task HandleSolicitadoAsync(EventoOutbox evento)
        {
            if (string.IsNullOrWhiteSpace(evento.Payload))
            {
                _logger.LogWarning("ProduccionTrabajoSolicitado sin payload. Outbox {Id}", evento.IdEventoOutbox);
                return;
            }

            ProduccionTrabajoSolicitadoEvent? solicitud;
            try
            {
                solicitud = JsonSerializer.Deserialize<ProduccionTrabajoSolicitadoEvent>(evento.Payload, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo deserializar ProduccionTrabajoSolicitado. Outbox {Id}", evento.IdEventoOutbox);
                throw;
            }

            if (solicitud == null)
                return;

            CompletarMetadatos(solicitud, evento);

            var creado = await _trabajos.CrearDesdeEventoAsync(solicitud);
            await EmitirSiHayAsync(creado);
        }

        private async Task HandleActualizadoAsync(EventoOutbox evento)
        {
            if (string.IsNullOrWhiteSpace(evento.Payload))
            {
                _logger.LogWarning("ProduccionTrabajoActualizado sin payload. Outbox {Id}", evento.IdEventoOutbox);
                return;
            }

            ProduccionTrabajoActualizadoEvent? solicitud;
            try
            {
                solicitud = JsonSerializer.Deserialize<ProduccionTrabajoActualizadoEvent>(evento.Payload, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo deserializar ProduccionTrabajoActualizado. Outbox {Id}", evento.IdEventoOutbox);
                throw;
            }

            if (solicitud == null)
                return;

            CompletarMetadatos(solicitud, evento);

            var actualizado = await _trabajos.ActualizarDesdeEventoAsync(solicitud);
            await EmitirSiHayAsync(actualizado);
        }

        private static void CompletarMetadatos(DomainEventBase solicitud, EventoOutbox evento)
        {
            if (solicitud.IdEmpresa <= 0)
                solicitud.IdEmpresa = evento.IdEmpresa;

            if (solicitud is ProduccionTrabajoSolicitadoEvent s)
            {
                if (s.OrigenId <= 0 && evento.ReferenciaId.HasValue && evento.ReferenciaId.Value > 0)
                    s.OrigenId = evento.ReferenciaId.Value;
                if (string.IsNullOrWhiteSpace(s.OrigenTipo) && !string.IsNullOrWhiteSpace(evento.ReferenciaTipo))
                    s.OrigenTipo = evento.ReferenciaTipo;
            }
            else if (solicitud is ProduccionTrabajoActualizadoEvent a)
            {
                if (a.OrigenId <= 0 && evento.ReferenciaId.HasValue && evento.ReferenciaId.Value > 0)
                    a.OrigenId = evento.ReferenciaId.Value;
                if (string.IsNullOrWhiteSpace(a.OrigenTipo) && !string.IsNullOrWhiteSpace(evento.ReferenciaTipo))
                    a.OrigenTipo = evento.ReferenciaTipo;
            }
        }

        private async Task EmitirSiHayAsync(ProduccionTrabajoDto? trabajo)
        {
            if (trabajo == null) return;
            try
            {
                await _realtime.EmitirTrabajoUpsertAsync(trabajo);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo emitir SignalR para trabajo {Id}", trabajo.IdTrabajo);
            }
        }
    }
}
