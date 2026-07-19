using AlahiaPos.Entities.Dto;

namespace AlahiaPos.Entities.Interfaces
{
    /// <summary>
    /// Único punto de evaluación de flags fiscales por empresa.
    /// El resto del código NO debe leer FiscalActivo directamente.
    /// </summary>
    public interface IFiscalFeatureService
    {
        Task<FiscalFeatureFlags> GetFeaturesAsync(int idEmpresa);
        Task InvalidateCacheAsync(int idEmpresa);
    }

    public interface ITaxClassificationService
    {
        byte? SugerirDestinoItbisCompra(bool tieneProductosInventariables, bool esServicioEstimado);
        byte? ResolverFormaVentaFiscal(string? tipoFactura);
        byte ResolverTipoIngresoDefault();
    }

    public interface IFiscalDocumentSnapshotService
    {
        Task ApplyVentaAsync(int idEmpresa, int idFacturaHeader, int idUsuario, bool forceReprocess = false);
        Task ApplyNotaCreditoAsync(int idEmpresa, int idNotaCredito, int idUsuario, bool forceReprocess = false);
        Task ApplyCompraAsync(int idEmpresa, int idOrdenCompraHeader, int idUsuario, bool forceReprocess = false);
        Task MarkErrorAsync(int idEmpresa, string tipoDocumento, int referenciaId, string mensaje);
        Task MarkPendienteGenerarAsync(int idEmpresa, string tipoDocumento, int referenciaId);
    }

    /// <summary>
    /// Encola trabajo fiscal si FiscalActivo. No-op total si fiscal off.
    /// No despacha handlers en el request HTTP.
    /// </summary>
    public interface IFiscalWorkEnqueueService
    {
        /// <summary>
        /// IdempotencyKey = FiscalFoto:{IdEmpresa}:{TipoDocumento}:{ReferenciaId}
        /// </summary>
        Task EnqueueFotografiaSiActivoAsync(FiscalDocumentoRequest request);
    }

    /// <summary>Fachada de procesamiento (worker / reproceso explícito).</summary>
    public interface IDgiiFiscalService
    {
        Task ProcesarDocumentoPosteriorAsync(FiscalDocumentoRequest request);
        Task ReprocesarAsync(FiscalDocumentoRequest request);
    }

    public interface IFiscalOutboxProcessor
    {
        Task<int> ProcessBatchAsync(CancellationToken ct, int batchSize = 10);
    }

    public interface IDgiiFiscalAuthService
    {
        Task EnsureCanManageConfigAsync(string? bearerToken, int idEmpresa, int idUsuarioClaimed);
        Task EnsureCanReprocessAsync(string? bearerToken, int idEmpresa, int idUsuarioClaimed);
    }

    public interface IFiscalReconciliacionService
    {
        Task<IReadOnlyList<FiscalReconciliacionItemDto>> ListarPendientesAsync(int idEmpresa, int top = 100);
    }

    public interface IDgiiConfigService
    {
        Task<DgiiConfiguracionEmpresaDto> GetAsync(int idEmpresa);
        Task<DgiiConfiguracionEmpresaDto> UpsertAsync(DgiiConfiguracionEmpresaDto dto, int idUsuario, string? motivo = null);
    }
}
