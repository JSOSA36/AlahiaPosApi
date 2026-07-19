using System.Security.Cryptography;
using System.Text;

namespace AlahiaPos.DataAccess.Servicios.FiscalGateway.Transmission
{
    public static class TransmissionProviderCodes
    {
        public const string Dgii = "DGII";
    }

    /// <summary>Resultado canónico del Provider hacia el Engine.</summary>
    public enum ProviderOutcome
    {
        AcceptedImmediate = 1,
        AcceptedPending = 2,
        RejectedBusiness = 3,
        TransientFailure = 4,
        Ambiguous = 5
    }

    public enum ProviderStatusOutcome
    {
        Pending = 1,
        Accepted = 2,
        AcceptedConditional = 3,
        Rejected = 4,
        NotFound = 5,
        Unknown = 6
    }

    /// <summary>Estados internos canónicos del Engine (no jerga de organismo).</summary>
    public static class TransmissionJobStates
    {
        public const string Queued = "Queued";
        public const string Sending = "Sending";
        public const string PendingProvider = "PendingProvider";
        public const string Accepted = "Accepted";
        public const string AcceptedConditional = "AcceptedConditional";
        public const string Rejected = "Rejected";
        public const string TransientPendingRetry = "TransientPendingRetry";
        public const string DeadlineExceeded = "DeadlineExceeded";
        public const string RequiresIntervention = "RequiresIntervention";

        public static bool IsTerminal(string state) =>
            state is Accepted or AcceptedConditional or Rejected or DeadlineExceeded;
    }

    public sealed class TransmissionPackage
    {
        public string ProviderCode { get; init; } = TransmissionProviderCodes.Dgii;
        public string TaxpayerId { get; init; } = "";
        public string DocumentId { get; init; } = "";
        public string? DocumentTypeHint { get; init; }
        public string ProviderChannel { get; init; } = "ECF";
        public string Payload { get; init; } = "";
        public string PayloadContentType { get; init; } = "application/xml";
        public string PayloadSha256 { get; init; } = "";
        public DateTime IssuedAtUtc { get; init; }
        public string? CorrelationId { get; init; }
        public int TenantId { get; init; }
        public string? SourceDocumentId { get; init; }
        public Dictionary<string, string> ProviderMetadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public static string ComputeSha256(string payload)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? ""));
            return Convert.ToHexString(hash);
        }
    }

    public sealed class ProviderSendResult
    {
        public ProviderOutcome Outcome { get; init; }
        public string? ProviderReceiptId { get; init; }
        public string? ProviderStatusCode { get; init; }
        public string? ProviderStatusLabel { get; init; }
        public List<string> Messages { get; init; } = new();
        public string? RawResponse { get; init; }
        public int? HttpStatusCode { get; init; }
    }

    public sealed class ProviderStatusResult
    {
        public ProviderStatusOutcome Outcome { get; init; }
        public string? ProviderReceiptId { get; init; }
        public string? ProviderStatusCode { get; init; }
        public string? ProviderStatusLabel { get; init; }
        public List<string> Messages { get; init; } = new();
        public string? RawResponse { get; init; }
    }

    public sealed class TransmissionAttemptRecord
    {
        public Guid AttemptId { get; init; } = Guid.NewGuid();
        public DateTime Utc { get; init; } = DateTime.UtcNow;
        public string Origin { get; init; } = "AutoWorker";
        public ProviderOutcome? Outcome { get; init; }
        public int? HttpStatusCode { get; init; }
        public string? ProviderReceiptId { get; init; }
        public string? ProviderStatusLabel { get; init; }
        public List<string> Messages { get; init; } = new();
        public int DurationMs { get; init; }
    }

    public sealed class TransmissionJob
    {
        public Guid JobId { get; init; } = Guid.NewGuid();
        public TransmissionPackage Package { get; init; } = null!;
        public string State { get; set; } = TransmissionJobStates.Queued;
        public string? ProviderReceiptId { get; set; }
        public string? ProviderStatusLabel { get; set; }
        public string? LastError { get; set; }
        public int AttemptCount { get; set; }
        public int StatusPollCount { get; set; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? NextAttemptUtc { get; set; }
        public DateTime DeadlineUtc { get; set; }
        public DateTime? LockedUntilUtc { get; set; }
        public string? LockedBy { get; set; }
        public List<TransmissionAttemptRecord> Attempts { get; } = new();
        /// <summary>Extras de negocio (securityCode, urlQR) no interpretados por el Engine.</summary>
        public Dictionary<string, string> ResultExtras { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class TransmissionAcceptResult
    {
        public Guid JobId { get; init; }
        public string State { get; init; } = "";
        public string? ProviderReceiptId { get; init; }
        public string? ProviderStatusLabel { get; init; }
        public List<string> Messages { get; init; } = new();
        public bool AcceptedForProcessing { get; init; }
        public Dictionary<string, string> Extras { get; init; } = new();
    }
}
