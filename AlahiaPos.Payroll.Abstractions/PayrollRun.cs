namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Ejecución de nómina (borrador o aprobado). Sin persistencia aquí — solo dominio.
/// </summary>
public sealed record PayrollRun(
    Guid PayrollRunId,
    int IdEmpresa,
    ExecutionKey ExecutionKey,
    PayrollPeriod Period,
    string RulePackId,
    string RulePackVersion,
    PayrollRunStatus Status,
    MoneyPolicy Money,
    IReadOnlyList<PayrollLine> Lines,
    IReadOnlyList<CalculationTrace> Traces,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ApprovedAt = null,
    string? SnapshotHash = null);
