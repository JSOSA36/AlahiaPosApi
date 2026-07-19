namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    public class TransmissionEngineOptions
    {
        public const string SectionName = "TransmissionEngine";

        public bool SyncAttemptEnabled { get; set; } = true;
        public int SyncTimeoutSeconds { get; set; } = 45;
        public int MaxWorkers { get; set; } = 2;
        public int BatchSize { get; set; } = 10;
        public int WorkerPollSeconds { get; set; } = 5;
        public TransmissionRetryOptions Retry { get; set; } = new();
        public TransmissionAlertOptions Alerts { get; set; } = new();
        public TransmissionIdempotencyOptions Idempotency { get; set; } = new();
    }

    public class TransmissionRetryOptions
    {
        public int InitialDelaySeconds { get; set; } = 60;
        public double BackoffMultiplier { get; set; } = 2.0;
        public int MaxDelaySeconds { get; set; } = 3600;
        public int JitterPercent { get; set; } = 20;
        public int MaxAttempts { get; set; } = 40;
    }

    public class TransmissionAlertOptions
    {
        public bool OnRequiresIntervention { get; set; } = true;
        public bool OnApproachingDeadline { get; set; } = true;
        public int OnWorkerHeartFailureMinutes { get; set; } = 10;
    }

    public class TransmissionIdempotencyOptions
    {
        public bool RejectDuplicateActiveJob { get; set; } = true;
        public bool AllowManualRetryIfNoTerminalState { get; set; } = true;
    }

    public class TransmissionProvidersOptions
    {
        public const string SectionName = "TransmissionProviders";
        public DgiiTransmissionProviderOptions DGII { get; set; } = new();
    }

    public class DgiiTransmissionProviderOptions
    {
        public bool Enabled { get; set; } = true;
        public int DeadlineHours { get; set; } = 72;
        public int AlertBeforeDeadlineHours { get; set; } = 6;
        public int HttpTimeoutSeconds { get; set; } = 60;
        public DgiiStatusQueryOptions StatusQuery { get; set; } = new();
    }

    public class DgiiStatusQueryOptions
    {
        public bool Enabled { get; set; } = true;
        public int InitialDelaySeconds { get; set; } = 5;
        public int IntervalSeconds { get; set; } = 15;
        public int MaxPolls { get; set; } = 40;
        public int GiveUpAfterMinutes { get; set; } = 30;
    }
}
