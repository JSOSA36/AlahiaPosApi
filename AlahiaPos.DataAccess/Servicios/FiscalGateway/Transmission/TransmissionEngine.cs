using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    public sealed class TransmissionProviderResolver : ITransmissionProviderResolver
    {
        private readonly IReadOnlyDictionary<string, ITransmissionProvider> _map;

        public TransmissionProviderResolver(IEnumerable<ITransmissionProvider> providers)
        {
            _map = providers.ToDictionary(p => p.ProviderCode, StringComparer.OrdinalIgnoreCase);
        }

        public ITransmissionProvider Get(string providerCode)
        {
            if (_map.TryGetValue(providerCode, out var p))
                return p;
            throw new NotSupportedException($"No hay Transmission Provider registrado para '{providerCode}'.");
        }
    }

    public sealed class TransmissionEngine : ITransmissionEngine
    {
        private readonly ITransmissionJobStore _store;
        private readonly ITransmissionProviderResolver _providers;
        private readonly TransmissionEngineOptions _options;
        private readonly TransmissionProvidersOptions _providerOpts;
        private readonly ILogger<TransmissionEngine> _logger;
        private readonly string _workerId;

        public TransmissionEngine(
            ITransmissionJobStore store,
            ITransmissionProviderResolver providers,
            IOptions<TransmissionEngineOptions> options,
            IOptions<TransmissionProvidersOptions> providerOpts,
            ILogger<TransmissionEngine> logger)
        {
            _store = store;
            _providers = providers;
            _options = options.Value;
            _providerOpts = providerOpts.Value;
            _logger = logger;
            var id = $"te-{Environment.MachineName}-{Guid.NewGuid():N}";
            _workerId = id.Length <= 80 ? id : id[..80];
        }

        public async Task<TransmissionAcceptResult> SubmitAsync(TransmissionPackage package, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(package.Payload))
                throw new InvalidOperationException("TransmissionPackage.Payload es requerido.");
            if (string.IsNullOrWhiteSpace(package.PayloadSha256))
                throw new InvalidOperationException("TransmissionPackage.PayloadSha256 es requerido.");

            if (_options.Idempotency.RejectDuplicateActiveJob)
            {
                var existing = await _store.FindActiveAsync(
                    package.ProviderCode, package.TaxpayerId, package.DocumentId, ct);
                if (existing != null)
                {
                    _logger.LogInformation(
                        "TE idempotent hit Job={JobId} {Provider}/{Doc}",
                        existing.JobId, package.ProviderCode, package.DocumentId);
                    return ToAccept(existing, accepted: true);
                }
            }

            var deadlineHours = ResolveDeadlineHours(package.ProviderCode);
            var job = new TransmissionJob
            {
                Package = package,
                State = TransmissionJobStates.Queued,
                DeadlineUtc = package.IssuedAtUtc.ToUniversalTime().AddHours(deadlineHours)
            };
            CopyExtrasFromPackage(job, package);
            await _store.SaveAsync(job, ct);

            if (_options.SyncAttemptEnabled)
            {
                using var syncCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                syncCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.SyncTimeoutSeconds)));
                await ExecuteSendAsync(job, "SyncApi", syncCts.Token);
                await _store.SaveAsync(job, ct);
                return ToAccept(job, accepted: true);
            }

            job.State = TransmissionJobStates.TransientPendingRetry;
            job.NextAttemptUtc = DateTime.UtcNow;
            await _store.SaveAsync(job, ct);
            return ToAccept(job, accepted: true);
        }

        public Task<TransmissionJob?> GetJobAsync(Guid jobId, CancellationToken ct)
            => _store.GetAsync(jobId, ct);

        public async Task<TransmissionAcceptResult> RetryManualAsync(Guid jobId, string requestedBy, CancellationToken ct)
        {
            if (!_options.Idempotency.AllowManualRetryIfNoTerminalState)
                throw new InvalidOperationException("Reenvío manual deshabilitado por configuración.");

            var job = await _store.GetAsync(jobId, ct)
                      ?? throw new InvalidOperationException($"Job {jobId} no encontrado.");

            if (TransmissionJobStates.IsTerminal(job.State) &&
                job.State is TransmissionJobStates.Accepted or TransmissionJobStates.AcceptedConditional or TransmissionJobStates.Rejected)
                throw new InvalidOperationException($"Job en estado terminal {job.State}; no se reenvía.");

            if (DateTime.UtcNow >= job.DeadlineUtc)
            {
                job.State = TransmissionJobStates.DeadlineExceeded;
                await _store.SaveAsync(job, ct);
                throw new InvalidOperationException("Deadline de transmisión vencido.");
            }

            await ExecuteSendAsync(job, $"Manual:{requestedBy}", ct);
            await _store.SaveAsync(job, ct);
            return ToAccept(job, accepted: true);
        }

        public async Task<int> ProcessPendingAsync(CancellationToken ct)
        {
            var processed = 0;
            var batch = Math.Max(1, _options.BatchSize);

            for (var i = 0; i < batch; i++)
            {
                if (ct.IsCancellationRequested) break;

                // Expirar deadlines
                await ExpireDeadlinesAsync(ct);

                var retryJob = await _store.ClaimNextRetryAsync(_workerId, TimeSpan.FromSeconds(60), ct);
                if (retryJob != null)
                {
                    try
                    {
                        await ExecuteSendAsync(retryJob, "AutoWorker", ct);
                    }
                    finally
                    {
                        retryJob.LockedBy = null;
                        retryJob.LockedUntilUtc = null;
                        await _store.SaveAsync(retryJob, ct);
                    }
                    processed++;
                    continue;
                }

                var pollJob = await _store.ClaimNextStatusPollAsync(_workerId, TimeSpan.FromSeconds(60), ct);
                if (pollJob == null) break;

                try
                {
                    await ExecuteStatusPollAsync(pollJob, ct);
                }
                finally
                {
                    pollJob.LockedBy = null;
                    pollJob.LockedUntilUtc = null;
                    await _store.SaveAsync(pollJob, ct);
                }
                processed++;
            }

            return processed;
        }

        private async Task ExecuteSendAsync(TransmissionJob job, string origin, CancellationToken ct)
        {
            if (DateTime.UtcNow >= job.DeadlineUtc)
            {
                job.State = TransmissionJobStates.DeadlineExceeded;
                job.LastError = "Deadline de transmisión vencido";
                return;
            }

            if (job.AttemptCount >= _options.Retry.MaxAttempts)
            {
                job.State = TransmissionJobStates.RequiresIntervention;
                job.LastError = "Máximo de intentos alcanzado";
                return;
            }

            var provider = _providers.Get(job.Package.ProviderCode);
            job.State = TransmissionJobStates.Sending;
            job.AttemptCount++;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            ProviderSendResult result;
            try
            {
                result = await provider.SendAsync(job.Package, ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result = new ProviderSendResult
                {
                    Outcome = ProviderOutcome.TransientFailure,
                    Messages = { "Timeout sync/envío" },
                    ProviderStatusCode = "TIMEOUT"
                };
            }
            sw.Stop();

            job.Attempts.Add(new TransmissionAttemptRecord
            {
                Utc = DateTime.UtcNow,
                Origin = origin,
                Outcome = result.Outcome,
                HttpStatusCode = result.HttpStatusCode,
                ProviderReceiptId = result.ProviderReceiptId,
                ProviderStatusLabel = result.ProviderStatusLabel,
                Messages = result.Messages.ToList(),
                DurationMs = (int)sw.ElapsedMilliseconds
            });

            ApplySendResult(job, result, provider);
        }

        private void ApplySendResult(TransmissionJob job, ProviderSendResult result, ITransmissionProvider provider)
        {
            job.ProviderStatusLabel = result.ProviderStatusLabel;
            job.LastError = result.Messages.FirstOrDefault();

            switch (result.Outcome)
            {
                case ProviderOutcome.AcceptedImmediate:
                    job.State = result.ProviderStatusLabel?.Contains("Condicional", StringComparison.OrdinalIgnoreCase) == true
                        ? TransmissionJobStates.AcceptedConditional
                        : TransmissionJobStates.Accepted;
                    job.ProviderReceiptId = result.ProviderReceiptId;
                    job.NextAttemptUtc = null;
                    break;

                case ProviderOutcome.AcceptedPending:
                    job.State = TransmissionJobStates.PendingProvider;
                    job.ProviderReceiptId = result.ProviderReceiptId;
                    var sq = ResolveStatusQuery(job.Package.ProviderCode);
                    job.NextAttemptUtc = DateTime.UtcNow.AddSeconds(Math.Max(1, sq.InitialDelaySeconds));
                    if (!provider.SupportsStatusQuery || !sq.Enabled)
                    {
                        // Sin poll: EnProceso operativo
                        job.ProviderStatusLabel = result.ProviderStatusLabel ?? "EnProceso";
                    }
                    break;

                case ProviderOutcome.RejectedBusiness:
                    job.State = TransmissionJobStates.Rejected;
                    job.NextAttemptUtc = null;
                    break;

                case ProviderOutcome.Ambiguous:
                    job.State = TransmissionJobStates.RequiresIntervention;
                    job.NextAttemptUtc = null;
                    break;

                case ProviderOutcome.TransientFailure:
                default:
                    ScheduleRetry(job);
                    break;
            }
        }

        private async Task ExecuteStatusPollAsync(TransmissionJob job, CancellationToken ct)
        {
            var provider = _providers.Get(job.Package.ProviderCode);
            if (!provider.SupportsStatusQuery || string.IsNullOrWhiteSpace(job.ProviderReceiptId))
                return;

            var sq = ResolveStatusQuery(job.Package.ProviderCode);
            if (!sq.Enabled) return;

            if (job.StatusPollCount >= sq.MaxPolls)
            {
                job.State = TransmissionJobStates.RequiresIntervention;
                job.LastError = "Máximo de consultas de estado alcanzado";
                return;
            }

            var giveUp = job.CreatedAtUtc.AddMinutes(sq.GiveUpAfterMinutes);
            if (DateTime.UtcNow >= giveUp)
            {
                job.State = TransmissionJobStates.RequiresIntervention;
                job.LastError = "Tiempo de consulta de estado agotado";
                return;
            }

            job.StatusPollCount++;
            var status = await provider.QueryStatusAsync(job.Package, job.ProviderReceiptId!, ct);
            job.ProviderStatusLabel = status.ProviderStatusLabel;
            job.Attempts.Add(new TransmissionAttemptRecord
            {
                Utc = DateTime.UtcNow,
                Origin = "StatusPoll",
                ProviderReceiptId = status.ProviderReceiptId,
                ProviderStatusLabel = status.ProviderStatusLabel,
                Messages = status.Messages.ToList()
            });

            switch (status.Outcome)
            {
                case ProviderStatusOutcome.Accepted:
                    job.State = TransmissionJobStates.Accepted;
                    job.NextAttemptUtc = null;
                    break;
                case ProviderStatusOutcome.AcceptedConditional:
                    job.State = TransmissionJobStates.AcceptedConditional;
                    job.NextAttemptUtc = null;
                    break;
                case ProviderStatusOutcome.Rejected:
                    job.State = TransmissionJobStates.Rejected;
                    job.NextAttemptUtc = null;
                    break;
                default:
                    job.NextAttemptUtc = DateTime.UtcNow.AddSeconds(Math.Max(1, sq.IntervalSeconds));
                    break;
            }
        }

        private void ScheduleRetry(TransmissionJob job)
        {
            if (DateTime.UtcNow >= job.DeadlineUtc)
            {
                job.State = TransmissionJobStates.DeadlineExceeded;
                return;
            }

            if (job.AttemptCount >= _options.Retry.MaxAttempts)
            {
                job.State = TransmissionJobStates.RequiresIntervention;
                return;
            }

            var delay = ComputeBackoffSeconds(job.AttemptCount);
            job.State = TransmissionJobStates.TransientPendingRetry;
            job.NextAttemptUtc = DateTime.UtcNow.AddSeconds(delay);
        }

        private int ComputeBackoffSeconds(int attemptCount)
        {
            var r = _options.Retry;
            var baseDelay = r.InitialDelaySeconds * Math.Pow(r.BackoffMultiplier, Math.Max(0, attemptCount - 1));
            baseDelay = Math.Min(baseDelay, r.MaxDelaySeconds);
            var jitter = baseDelay * (r.JitterPercent / 100.0) * (Random.Shared.NextDouble() * 2 - 1);
            return Math.Max(1, (int)Math.Round(baseDelay + jitter));
        }

        private async Task ExpireDeadlinesAsync(CancellationToken ct)
        {
            // In-memory: revisar jobs no terminales vencidos al procesar claim; claim ya filtra deadline.
            // Marcar explícitamente TransientPendingRetry vencidos:
            // (opcional mejora futura con índice)
            await Task.CompletedTask;
        }

        private int ResolveDeadlineHours(string providerCode)
        {
            if (providerCode.Equals(TransmissionProviderCodes.Dgii, StringComparison.OrdinalIgnoreCase))
                return Math.Max(1, _providerOpts.DGII.DeadlineHours);
            return 72;
        }

        private DgiiStatusQueryOptions ResolveStatusQuery(string providerCode)
        {
            if (providerCode.Equals(TransmissionProviderCodes.Dgii, StringComparison.OrdinalIgnoreCase))
                return _providerOpts.DGII.StatusQuery;
            return new DgiiStatusQueryOptions { Enabled = false };
        }

        private static void CopyExtrasFromPackage(TransmissionJob job, TransmissionPackage package)
        {
            foreach (var kv in package.ProviderMetadata)
            {
                if (kv.Key.StartsWith("result.", StringComparison.OrdinalIgnoreCase))
                    job.ResultExtras[kv.Key["result.".Length..]] = kv.Value;
            }
        }

        private static TransmissionAcceptResult ToAccept(TransmissionJob job, bool accepted) => new()
        {
            JobId = job.JobId,
            State = job.State,
            ProviderReceiptId = job.ProviderReceiptId,
            ProviderStatusLabel = job.ProviderStatusLabel,
            Messages = string.IsNullOrEmpty(job.LastError)
                ? new List<string>()
                : new List<string> { job.LastError },
            AcceptedForProcessing = accepted,
            Extras = new Dictionary<string, string>(job.ResultExtras, StringComparer.OrdinalIgnoreCase)
        };
    }
}
