using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Encola fotografía fiscal solo si FiscalActivo.
    /// No-op total si no hay config o FiscalActivo=false (cero outbox, cero UPDATE).
    /// No despacha handlers en el request.
    /// IdempotencyKey = FiscalFoto:{IdEmpresa}:{Tipo}:{ReferenciaId}
    /// </summary>
    public class FiscalWorkEnqueueService : IFiscalWorkEnqueueService
    {
        public const string IdempotencyPrefix = "FiscalFoto";

        private readonly AlahiaPosContext _context;
        private readonly IFiscalFeatureService _features;
        private readonly IFiscalDocumentSnapshotService _snapshot;
        private readonly ILogger<FiscalWorkEnqueueService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public FiscalWorkEnqueueService(
            AlahiaPosContext context,
            IFiscalFeatureService features,
            IFiscalDocumentSnapshotService snapshot,
            ILogger<FiscalWorkEnqueueService> logger)
        {
            _context = context;
            _features = features;
            _snapshot = snapshot;
            _logger = logger;
        }

        public static string BuildIdempotencyKey(int idEmpresa, string tipo, int referenciaId)
            => $"{IdempotencyPrefix}:{idEmpresa}:{NormalizarTipo(tipo)}:{referenciaId}";

        public async Task EnqueueFotografiaSiActivoAsync(FiscalDocumentoRequest request)
        {
            if (request == null || request.IdEmpresa <= 0 || request.ReferenciaId <= 0)
                return;

            var features = await _features.GetFeaturesAsync(request.IdEmpresa);
            if (!features.FiscalActivo)
                return; // no-op total

            var tipo = NormalizarTipo(request.TipoDocumento);
            if (tipo == "Venta" || tipo == "NotaCredito")
            {
                if (!features.FotoVentaRelevante)
                    return;
            }
            else if (tipo == "Compra")
            {
                if (!features.FotoCompraRelevante)
                    return;
            }

            var key = BuildIdempotencyKey(request.IdEmpresa, tipo, request.ReferenciaId);

            // Ya hay trabajo activo → no duplicar
            var activo = await _context.EventosOutbox.AsNoTracking().AnyAsync(e =>
                e.IdempotencyKey == key
                && (e.Estado == EventoOutboxEstados.Pendiente
                    || e.Estado == EventoOutboxEstados.Procesando));
            if (activo)
                return;

            if (!request.ForceReprocess)
            {
                // Foto válida ya existe → no encolar
                if (await TieneFotoValidaAsync(request.IdEmpresa, tipo, request.ReferenciaId))
                    return;
            }

            var payload = JsonSerializer.Serialize(request, JsonOptions);

            _context.EventosOutbox.Add(new EventoOutbox
            {
                IdEmpresa = request.IdEmpresa,
                TipoEvento = DomainEventTypes.FiscalFotografiaPendiente,
                ReferenciaId = request.ReferenciaId,
                ReferenciaTipo = tipo,
                Payload = payload,
                Estado = EventoOutboxEstados.Pendiente,
                Intentos = 0,
                FechaCreacion = DateTime.Now,
                IdempotencyKey = key
            });

            await _context.SaveChangesAsync();

            // Documento queda pendiente de worker (un solo UPDATE al encolar)
            try
            {
                await _snapshot.MarkPendienteGenerarAsync(request.IdEmpresa, tipo, request.ReferenciaId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo marcar PENDIENTE_GENERAR (outbox ya encolado). Empresa={E} Ref={R}",
                    request.IdEmpresa, request.ReferenciaId);
            }
        }

        private async Task<bool> TieneFotoValidaAsync(int idEmpresa, string tipo, int referenciaId)
        {
            var ok = new[]
            {
                EstadoFiscalDocumentoConstantes.Generada,
                EstadoFiscalDocumentoConstantes.PendienteValidar
            };

            return tipo switch
            {
                "Venta" => await _context.FacturaHeaders.AsNoTracking().AnyAsync(h =>
                    h.IdFacturaHeader == referenciaId && h.IdEmpresa == idEmpresa
                    && h.EstadoFiscalDocumento != null && ok.Contains(h.EstadoFiscalDocumento)),
                "NotaCredito" => await _context.NotasCredito.AsNoTracking().AnyAsync(h =>
                    h.IdNotaCredito == referenciaId && h.IdEmpresa == idEmpresa
                    && h.EstadoFiscalDocumento != null && ok.Contains(h.EstadoFiscalDocumento)),
                "Compra" => await _context.OrdenCompraHeaders.AsNoTracking().AnyAsync(h =>
                    h.IdOrdenCompraHeader == referenciaId && h.IdEmpresa == idEmpresa
                    && h.EstadoFiscalDocumento != null && ok.Contains(h.EstadoFiscalDocumento)),
                _ => false
            };
        }

        private static string NormalizarTipo(string? tipo)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return "Venta";
            var t = tipo.Trim();
            if (t.Equals("NotaCredito", StringComparison.OrdinalIgnoreCase) || t.Equals("NC", StringComparison.OrdinalIgnoreCase))
                return "NotaCredito";
            if (t.Equals("Compra", StringComparison.OrdinalIgnoreCase) || t.Equals("FACTC", StringComparison.OrdinalIgnoreCase))
                return "Compra";
            return "Venta";
        }
    }
}
