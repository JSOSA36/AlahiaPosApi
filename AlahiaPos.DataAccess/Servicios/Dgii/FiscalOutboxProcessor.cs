using AlahiaPos.DataAccess.Data;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Events;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Claim atómico + procesamiento idempotente de FiscalFotografiaPendiente.
    /// </summary>
    public class FiscalOutboxProcessor : IFiscalOutboxProcessor
    {
        public const int MaxIntentos = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(60);

        private readonly AlahiaPosContext _context;
        private readonly IDgiiFiscalService _dgiiFiscal;
        private readonly ILogger<FiscalOutboxProcessor> _logger;
        private readonly string _workerId;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public FiscalOutboxProcessor(
            AlahiaPosContext context,
            IDgiiFiscalService dgiiFiscal,
            ILogger<FiscalOutboxProcessor> logger)
        {
            _context = context;
            _dgiiFiscal = dgiiFiscal;
            _logger = logger;
            var id = $"fw-{Environment.MachineName}-{Guid.NewGuid():N}";
            _workerId = id.Length > 100 ? id[..100] : id;
        }

        public async Task<int> ProcessBatchAsync(CancellationToken ct, int batchSize = 10)
        {
            var processed = 0;
            for (var i = 0; i < batchSize; i++)
            {
                if (ct.IsCancellationRequested) break;
                var id = await ClaimNextIdAsync(ct);
                if (id == null) break;

                var claimed = await _context.EventosOutbox.AsTracking()
                    .FirstOrDefaultAsync(e => e.IdEventoOutbox == id.Value, ct);
                if (claimed == null) continue;

                try
                {
                    var request = Deserialize(claimed);
                    await _dgiiFiscal.ProcesarDocumentoPosteriorAsync(request);

                    claimed.Estado = EventoOutboxEstados.Procesado;
                    claimed.FechaProcesado = DateTime.Now;
                    claimed.MensajeError = null;
                    claimed.LockedUntil = null;
                    claimed.LockedBy = null;
                    await _context.SaveChangesAsync(ct);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fallo procesando outbox fiscal Id={Id}", claimed.IdEventoOutbox);
                    claimed.Estado = claimed.Intentos >= MaxIntentos
                        ? EventoOutboxEstados.Error
                        : EventoOutboxEstados.Pendiente;
                    claimed.MensajeError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                    claimed.LockedUntil = null;
                    claimed.LockedBy = null;
                    claimed.FechaProcesado = DateTime.Now;
                    await _context.SaveChangesAsync(ct);
                }
            }

            return processed;
        }

        private async Task<int?> ClaimNextIdAsync(CancellationToken ct)
        {
            var lockUntil = DateTime.Now.Add(LockDuration);
            // OUTPUT Id solo — claim exclusivo entre workers
            var sql = @"
DECLARE @claimed TABLE (Id INT);
;WITH cte AS (
    SELECT TOP (1) *
    FROM dbo.EventosOutbox WITH (ROWLOCK, READPAST, UPDLOCK)
    WHERE TipoEvento = @tipo
      AND Intentos < @maxIntentos
      AND (
            Estado = N'Pendiente'
         OR (Estado = N'Error' AND Intentos < @maxIntentos)
         OR (Estado = N'Procesando' AND (LockedUntil IS NULL OR LockedUntil < GETDATE()))
      )
    ORDER BY FechaCreacion
)
UPDATE cte
SET Estado = N'Procesando',
    LockedBy = @lockedBy,
    LockedUntil = @lockedUntil,
    Intentos = Intentos + 1,
    FechaProcesado = NULL
OUTPUT INSERTED.IdEventoOutbox INTO @claimed;
SELECT Id FROM @claimed;";

            await using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@lockedBy", _workerId));
            cmd.Parameters.Add(new SqlParameter("@lockedUntil", lockUntil));
            cmd.Parameters.Add(new SqlParameter("@tipo", DomainEventTypes.FiscalFotografiaPendiente));
            cmd.Parameters.Add(new SqlParameter("@maxIntentos", MaxIntentos));

            if (cmd.Connection!.State != System.Data.ConnectionState.Open)
                await cmd.Connection.OpenAsync(ct);

            var result = await cmd.ExecuteScalarAsync(ct);
            if (result == null || result == DBNull.Value) return null;
            return Convert.ToInt32(result);
        }

        private static FiscalDocumentoRequest Deserialize(EventoOutbox evento)
        {
            FiscalDocumentoRequest? req = null;
            try
            {
                req = JsonSerializer.Deserialize<FiscalDocumentoRequest>(evento.Payload, JsonOptions);
            }
            catch { /* fallback */ }

            return req ?? new FiscalDocumentoRequest
            {
                IdEmpresa = evento.IdEmpresa,
                ReferenciaId = evento.ReferenciaId ?? 0,
                TipoDocumento = evento.ReferenciaTipo ?? "Venta",
                IdUsuario = 0
            };
        }
    }
}
