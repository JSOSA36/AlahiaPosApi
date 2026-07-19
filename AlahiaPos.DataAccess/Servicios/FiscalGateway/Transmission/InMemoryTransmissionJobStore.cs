using System.Collections.Concurrent;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    /// <summary>
    /// Store durable en memoria del proceso. Suficiente para P0 de Alahia.eCF.Api;
    /// sustituible por SQL sin cambiar el Engine.
    /// </summary>
    public sealed class InMemoryTransmissionJobStore : ITransmissionJobStore
    {
        private readonly ConcurrentDictionary<Guid, TransmissionJob> _byId = new();
        private readonly object _gate = new();

        public Task<TransmissionJob?> FindActiveAsync(
            string providerCode, string taxpayerId, string documentId, CancellationToken ct)
        {
            lock (_gate)
            {
                var job = _byId.Values
                    .Where(j =>
                        j.Package.ProviderCode == providerCode &&
                        j.Package.TaxpayerId == taxpayerId &&
                        j.Package.DocumentId == documentId &&
                        !TransmissionJobStates.IsTerminal(j.State))
                    .OrderByDescending(j => j.CreatedAtUtc)
                    .FirstOrDefault();
                return Task.FromResult(job);
            }
        }

        public Task<TransmissionJob?> GetAsync(Guid jobId, CancellationToken ct)
        {
            _byId.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task SaveAsync(TransmissionJob job, CancellationToken ct)
        {
            job.UpdatedAtUtc = DateTime.UtcNow;
            _byId[job.JobId] = job;
            return Task.CompletedTask;
        }

        public Task<TransmissionJob?> ClaimNextRetryAsync(string workerId, TimeSpan lockDuration, CancellationToken ct)
        {
            lock (_gate)
            {
                var now = DateTime.UtcNow;
                var job = _byId.Values
                    .Where(j =>
                        j.State == TransmissionJobStates.TransientPendingRetry &&
                        (j.NextAttemptUtc == null || j.NextAttemptUtc <= now) &&
                        (j.LockedUntilUtc == null || j.LockedUntilUtc <= now) &&
                        now < j.DeadlineUtc)
                    .OrderBy(j => j.NextAttemptUtc ?? j.CreatedAtUtc)
                    .FirstOrDefault();

                if (job == null) return Task.FromResult<TransmissionJob?>(null);

                job.LockedBy = workerId;
                job.LockedUntilUtc = now.Add(lockDuration);
                job.State = TransmissionJobStates.Sending;
                job.UpdatedAtUtc = now;
                return Task.FromResult<TransmissionJob?>(job);
            }
        }

        public Task<TransmissionJob?> ClaimNextStatusPollAsync(string workerId, TimeSpan lockDuration, CancellationToken ct)
        {
            lock (_gate)
            {
                var now = DateTime.UtcNow;
                var job = _byId.Values
                    .Where(j =>
                        j.State == TransmissionJobStates.PendingProvider &&
                        !string.IsNullOrWhiteSpace(j.ProviderReceiptId) &&
                        (j.LockedUntilUtc == null || j.LockedUntilUtc <= now) &&
                        (j.NextAttemptUtc == null || j.NextAttemptUtc <= now))
                    .OrderBy(j => j.NextAttemptUtc ?? j.UpdatedAtUtc)
                    .FirstOrDefault();

                if (job == null) return Task.FromResult<TransmissionJob?>(null);

                job.LockedBy = workerId;
                job.LockedUntilUtc = now.Add(lockDuration);
                job.UpdatedAtUtc = now;
                return Task.FromResult<TransmissionJob?>(job);
            }
        }
    }
}
