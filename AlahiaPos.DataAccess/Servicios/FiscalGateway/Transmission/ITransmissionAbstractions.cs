namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    public interface ITransmissionProvider
    {
        string ProviderCode { get; }
        bool SupportsStatusQuery { get; }

        Task<ProviderSendResult> SendAsync(TransmissionPackage package, CancellationToken ct);
        Task<ProviderStatusResult> QueryStatusAsync(
            TransmissionPackage package,
            string providerReceiptId,
            CancellationToken ct);
    }

    public interface ITransmissionProviderResolver
    {
        ITransmissionProvider Get(string providerCode);
    }

    public interface ITransmissionJobStore
    {
        Task<TransmissionJob?> FindActiveAsync(string providerCode, string taxpayerId, string documentId, CancellationToken ct);
        Task<TransmissionJob?> GetAsync(Guid jobId, CancellationToken ct);
        Task SaveAsync(TransmissionJob job, CancellationToken ct);
        Task<TransmissionJob?> ClaimNextRetryAsync(string workerId, TimeSpan lockDuration, CancellationToken ct);
        Task<TransmissionJob?> ClaimNextStatusPollAsync(string workerId, TimeSpan lockDuration, CancellationToken ct);
    }

    public interface ITransmissionEngine
    {
        Task<TransmissionAcceptResult> SubmitAsync(TransmissionPackage package, CancellationToken ct);
        Task<TransmissionJob?> GetJobAsync(Guid jobId, CancellationToken ct);
        Task<TransmissionAcceptResult> RetryManualAsync(Guid jobId, string requestedBy, CancellationToken ct);
        /// <summary>Procesa un lote (reintentos + polls). Usado por el worker.</summary>
        Task<int> ProcessPendingAsync(CancellationToken ct);
    }
}
