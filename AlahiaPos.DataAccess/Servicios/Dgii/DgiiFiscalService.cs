using AlahiaPos.Entities.Dto;
using AlahiaPos.Entities.Interfaces;
using Microsoft.Extensions.Logging;

namespace AlahiaPos.DataAccess.Servicios.Dgii
{
    /// <summary>
    /// Procesa fotografía fiscal (worker / reproceso). Nunca propaga a la operación comercial.
    /// </summary>
    public class DgiiFiscalService : IDgiiFiscalService
    {
        private readonly IFiscalFeatureService _features;
        private readonly IFiscalDocumentSnapshotService _snapshot;
        private readonly ILogger<DgiiFiscalService> _logger;

        public DgiiFiscalService(
            IFiscalFeatureService features,
            IFiscalDocumentSnapshotService snapshot,
            ILogger<DgiiFiscalService> logger)
        {
            _features = features;
            _snapshot = snapshot;
            _logger = logger;
        }

        public Task ProcesarDocumentoPosteriorAsync(FiscalDocumentoRequest request)
            => ProcesarInternoAsync(request);

        public Task ReprocesarAsync(FiscalDocumentoRequest request)
        {
            request.ForceReprocess = true;
            return ProcesarInternoAsync(request);
        }

        private async Task ProcesarInternoAsync(FiscalDocumentoRequest request)
        {
            if (request == null || request.IdEmpresa <= 0 || request.ReferenciaId <= 0)
                return;

            try
            {
                var features = await _features.GetFeaturesAsync(request.IdEmpresa);
                if (!features.FiscalActivo)
                    return; // no-op

                var tipo = NormalizarTipo(request.TipoDocumento);
                var force = request.ForceReprocess;
                switch (tipo)
                {
                    case "Venta":
                        await _snapshot.ApplyVentaAsync(request.IdEmpresa, request.ReferenciaId, request.IdUsuario, force);
                        break;
                    case "NotaCredito":
                        await _snapshot.ApplyNotaCreditoAsync(request.IdEmpresa, request.ReferenciaId, request.IdUsuario, force);
                        break;
                    case "Compra":
                        await _snapshot.ApplyCompraAsync(request.IdEmpresa, request.ReferenciaId, request.IdUsuario, force);
                        break;
                    default:
                        _logger.LogWarning("Tipo fiscal desconocido: {Tipo}", request.TipoDocumento);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Fallo fiscal. Empresa={Empresa} Tipo={Tipo} Ref={Ref}",
                    request.IdEmpresa, request.TipoDocumento, request.ReferenciaId);

                try
                {
                    await _snapshot.MarkErrorAsync(
                        request.IdEmpresa,
                        NormalizarTipo(request.TipoDocumento),
                        request.ReferenciaId,
                        ex.Message);
                }
                catch (Exception markEx)
                {
                    _logger.LogError(markEx, "No se pudo marcar ERROR_FISCAL.");
                }

                throw; // worker marca Error en outbox
            }
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
