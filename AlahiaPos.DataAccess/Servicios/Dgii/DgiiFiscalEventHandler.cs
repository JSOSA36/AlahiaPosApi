using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Consumidor de eventos comerciales. Genera fotografía fiscal vía fachada.
    /// Nunca relanza excepciones hacia el dispatcher de forma que tumbe la venta
    /// (el publisher ya traga errores de dispatch; aquí también se protege).
    /// </summary>
    public class DgiiFiscalEventHandler : IDomainEventHandler
    {
        private readonly IDgiiFiscalService _dgiiFiscal;
        private readonly ILogger<DgiiFiscalEventHandler> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public DgiiFiscalEventHandler(
            IDgiiFiscalService dgiiFiscal,
            ILogger<DgiiFiscalEventHandler> logger)
        {
            _dgiiFiscal = dgiiFiscal;
            _logger = logger;
        }

        public async Task HandleAsync(EventoOutbox evento)
        {
            if (evento.TipoEvento != DomainEventTypes.DocumentoComercialConfirmado)
                return;

            try
            {
                var payload = JsonSerializer.Deserialize<DocumentoComercialConfirmadoEvent>(
                    evento.Payload, JsonOptions);

                var tipo = payload?.TipoDocumentoFiscal
                    ?? evento.ReferenciaTipo
                    ?? "Venta";

                await _dgiiFiscal.ProcesarDocumentoPosteriorAsync(new FiscalDocumentoRequest
                {
                    IdEmpresa = evento.IdEmpresa,
                    ReferenciaId = evento.ReferenciaId ?? payload?.ReferenciaId ?? 0,
                    IdUsuario = payload?.IdUsuario ?? 0,
                    TipoDocumento = tipo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "DgiiFiscalEventHandler falló (no afecta operación comercial). Evento={Id}",
                    evento.IdEventoOutbox);
            }
        }
    }
}
