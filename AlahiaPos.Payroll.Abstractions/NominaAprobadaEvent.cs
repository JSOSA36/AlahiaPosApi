namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Evento de dominio inmutable emitido tras aprobar un run (ALAHIA-PE-01 §14).
/// El detalle completo permanece en el snapshot del <see cref="PayrollRun"/>.
/// </summary>
public sealed record NominaAprobadaEvent(
    Guid PayrollRunId,
    int IdEmpresa,
    string PeriodKey,
    string RulePackId,
    string RulePackVersion,
    DateTimeOffset ApprovedAt,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal NetPay,
    decimal TotalEmployerContributions);
