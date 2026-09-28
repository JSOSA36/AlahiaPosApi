using AlahiaPos.DataAccess.Data;
using AlahiaPos.DataAccess.Servicios.FacturacionElectronica;
using AlahiaPos.Entities.Domain;
using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Dto.Fiscal;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway
{
    /// <summary>
    /// Procesador de outbox para eventos ECF_ENVIAR_GATEWAY.
    /// Lee el ECFEncabezado, construye FiscalDocumentoElectronico via resolver,
    /// envía al Gateway y actualiza estados.
    /// </summary>
    public class EcfGatewayOutboxProcessor
    {
        private const string TipoEvento = "ECF_ENVIAR_GATEWAY";
        private const int MaxIntentos = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(60);
        private readonly string _workerId;

        private readonly AlahiaPosContext _ctx;
        private readonly IFiscalGateway _gateway;
        private readonly IFiscalDocumentoValidator _validator;
        private readonly IDocumentoOrigenResolverFactory _resolverFactory;
        private readonly ISecuenciaEcfService _secuencias;
        private readonly ILogger<EcfGatewayOutboxProcessor> _logger;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public EcfGatewayOutboxProcessor(
            AlahiaPosContext ctx,
            IFiscalGateway gateway,
            IFiscalDocumentoValidator validator,
            IDocumentoOrigenResolverFactory resolverFactory,
            ISecuenciaEcfService secuencias,
            ILogger<EcfGatewayOutboxProcessor> logger)
        {
            _ctx = ctx;
            _gateway = gateway;
            _validator = validator;
            _resolverFactory = resolverFactory;
            _secuencias = secuencias;
            _logger = logger;
            var id = $"ecf-gw-{Environment.MachineName}-{Guid.NewGuid():N}";
            _workerId = id.Length > 100 ? id[..100] : id;
        }

        public async Task<int> ProcessBatchAsync(CancellationToken ct, int batchSize = 5)
        {
            var processed = 0;
            for (var i = 0; i < batchSize; i++)
            {
                if (ct.IsCancellationRequested) break;
                var id = await ClaimNextIdAsync(ct);
                if (id == null) break;

                var evento = await _ctx.EventosOutbox.AsTracking()
                    .FirstOrDefaultAsync(e => e.IdEventoOutbox == id.Value, ct);
                if (evento == null) continue;

                try
                {
                    await ProcessEventAsync(evento, ct);
                    evento.Estado = EventoOutboxEstados.Procesado;
                    evento.FechaProcesado = DateTime.Now;
                    evento.MensajeError = null;
                    evento.LockedUntil = null;
                    evento.LockedBy = null;
                    await _ctx.SaveChangesAsync(ct);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error procesando ECF Gateway outbox Id={Id}", evento.IdEventoOutbox);
                    evento.Estado = evento.Intentos >= MaxIntentos
                        ? EventoOutboxEstados.Error
                        : EventoOutboxEstados.Pendiente;
                    evento.MensajeError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                    evento.LockedUntil = null;
                    evento.LockedBy = null;
                    evento.FechaProcesado = DateTime.Now;
                    await _ctx.SaveChangesAsync(ct);
                }
            }
            return processed;
        }

        private async Task ProcessEventAsync(EventoOutbox evento, CancellationToken ct)
        {
            var payload = JsonSerializer.Deserialize<EcfOutboxPayload>(evento.Payload, JsonOpts)
                ?? throw new InvalidOperationException($"Payload inválido en outbox {evento.IdEventoOutbox}");

            var ecf = await _ctx.ECFEncabezados.AsTracking()
                .FirstOrDefaultAsync(e => e.IdECF == (evento.ReferenciaId ?? payload.IdEcf), ct)
                ?? throw new InvalidOperationException($"ECFEncabezado no encontrado para outbox {evento.IdEventoOutbox}");

            var empresa = await _ctx.Empresas.AsNoTracking()
                .FirstOrDefaultAsync(e => e.IdEmpresa == payload.IdEmpresa, ct);
            if (empresa == null)
                _logger.LogWarning("Empresa {IdEmpresa} no encontrada para ECF {Encf}", payload.IdEmpresa, ecf.ENCF);

            var origen = (OrigenDocumento)payload.OrigenDocumento;
            var resolver = _resolverFactory.Get(origen);
            var docInfo = await resolver.ObtenerDocumentoAsync(payload.IdOrigen, payload.IdEmpresa);
            var secuencia = await _secuencias.ObtenerActivaAsync(
                payload.IdEmpresa,
                payload.TipoEcfDgii,
                payload.IdSucursal ?? docInfo.IdSucursal);

            var docElectronico = FiscalDocumentoBuilder.Build(
                ecf, docInfo, payload.IdOrigen, payload.OrigenDocumento,
                payload.TipoEcfDgii, empresa, secuencia);

            var validacion = _validator.Validar(docElectronico);
            if (!validacion.Ok)
            {
                ecf.EstadoDocumento = EstadoDocumentoElectronico.Error;
                ecf.EstadoDGII = "Error";
                ecf.MensajeRespuesta = validacion.Mensaje;
                await _ctx.SaveChangesAsync(ct);
                _logger.LogWarning(
                    "ECF {Encf} bloqueado por validación previa (no enviado a gateway): {Error}",
                    ecf.ENCF, validacion.Mensaje);
                return;
            }

            _logger.LogInformation(
                "Gateway: enviando ECF {Encf} al proveedor (Origen={Origen}, IdOrigen={IdOrigen})",
                ecf.ENCF, origen, payload.IdOrigen);

            var resultado = await _gateway.EnviarDocumentoAsync(docElectronico, ct);
            AplicarResultado(ecf, resultado);
            await _ctx.SaveChangesAsync(ct);

            var reintentos = 0;
            while (!resultado.Exitoso
                && EcfSecuenciaYaUtilizada.EnResultado(resultado)
                && reintentos < MaxIntentos)
            {
                reintentos++;
                var reserva = await _secuencias.ReservarSiguienteAsync(
                    payload.IdEmpresa,
                    payload.TipoEcfDgii,
                    payload.IdSucursal ?? docInfo.IdSucursal);
                if (!reserva.Exitoso || string.IsNullOrWhiteSpace(reserva.Encf))
                {
                    _logger.LogError(
                        "ECF {Encf} ya utilizado y no hay secuencia siguiente: {Error}",
                        ecf.ENCF, reserva.MensajeError);
                    return;
                }

                _logger.LogWarning(
                    "ECF {EncfAnterior} ya utilizado. Outbox reintenta con {EncfNuevo} ({Intento}/{Max})",
                    ecf.ENCF, reserva.Encf, reintentos, MaxIntentos);

                var nuevo = new ECFEncabezado
                {
                    IdEmpresa = ecf.IdEmpresa,
                    TipoECF = ecf.TipoECF,
                    ENCF = reserva.Encf,
                    FechaEmision = ecf.FechaEmision,
                    RncEmisor = ecf.RncEmisor,
                    RncReceptor = ecf.RncReceptor,
                    NombreReceptor = ecf.NombreReceptor,
                    MontoGravado = ecf.MontoGravado,
                    TotalITBIS = ecf.TotalITBIS,
                    TotalGeneral = ecf.TotalGeneral,
                    OrigenDocumento = ecf.OrigenDocumento,
                    IdOrigen = ecf.IdOrigen,
                    NumeroFacturaInterna = ecf.NumeroFacturaInterna,
                    EstadoDocumento = EstadoDocumentoElectronico.PendienteEnvio,
                    EstadoDGII = "Pendiente",
                    FechaCreacion = DateTime.Now
                };
                _ctx.ECFEncabezados.Add(nuevo);
                await _ctx.SaveChangesAsync(ct);

                evento.ReferenciaId = nuevo.IdECF;
                ecf = nuevo;
                docElectronico = FiscalDocumentoBuilder.Build(
                    ecf, docInfo, payload.IdOrigen, payload.OrigenDocumento,
                    payload.TipoEcfDgii, empresa, secuencia);
                docElectronico.Encabezado.Encf = reserva.Encf;

                resultado = await _gateway.EnviarDocumentoAsync(docElectronico, ct);
                AplicarResultado(ecf, resultado);
                await _ctx.SaveChangesAsync(ct);
            }
        }

        private void AplicarResultado(ECFEncabezado ecf, FiscalEnvioResultado resultado)
        {
            ecf.TrackId = resultado.TrackId;
            if (!string.IsNullOrWhiteSpace(resultado.TransmissionJobId))
                ecf.TransmissionJobId = resultado.TransmissionJobId;
            ecf.SecurityCode = resultado.SecurityCode;
            ecf.UrlQR = resultado.UrlQR;
            ecf.FechaFirma = resultado.FechaFirma;
            ecf.FechaEnvio = DateTime.Now;

            if (resultado.Exitoso && !EcfSecuenciaYaUtilizada.EnResultado(resultado))
            {
                ecf.EstadoDocumento = EstadoDocumentoElectronico.Enviado;
                ecf.EstadoDGII = resultado.Estado;
                _logger.LogInformation("ECF {Encf} enviado exitosamente. TrackId={TrackId}", ecf.ENCF, resultado.TrackId);
            }
            else
            {
                ecf.EstadoDocumento = EstadoDocumentoElectronico.Error;
                ecf.EstadoDGII = string.IsNullOrWhiteSpace(resultado.Estado)
                    || (resultado.Estado ?? "").Contains("Aceptado", StringComparison.OrdinalIgnoreCase)
                    ? "Error"
                    : resultado.Estado;
                ecf.MensajeRespuesta = string.Join("; ", resultado.Mensajes);
                _logger.LogWarning("ECF {Encf} rechazado por proveedor: {Error}", ecf.ENCF, ecf.MensajeRespuesta);
            }
        }

        private async Task<int?> ClaimNextIdAsync(CancellationToken ct)
        {
            var lockUntil = DateTime.Now.Add(LockDuration);
            const string sql = @"
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

            var conn = _ctx.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new SqlParameter("@lockedBy", _workerId));
            cmd.Parameters.Add(new SqlParameter("@lockedUntil", lockUntil));
            cmd.Parameters.Add(new SqlParameter("@tipo", TipoEvento));
            cmd.Parameters.Add(new SqlParameter("@maxIntentos", MaxIntentos));

            var result = await cmd.ExecuteScalarAsync(ct);
            if (result == null || result == DBNull.Value) return null;
            return Convert.ToInt32(result);
        }

        private class EcfOutboxPayload
        {
            public int IdEmpresa { get; set; }
            public int IdEcf { get; set; }
            public int OrigenDocumento { get; set; }
            public int IdOrigen { get; set; }
            public int TipoEcfDgii { get; set; }
            public string? Encf { get; set; }
            public int IdUsuario { get; set; }
            public int? IdSucursal { get; set; }
        }
    }
}
