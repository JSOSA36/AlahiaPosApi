namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Identidad idempotente de una solicitud de cálculo (ALAHIA-PE-01 D18 / §7A).
/// </summary>
public sealed record ExecutionKey(
    int IdEmpresa,
    string PeriodKey,
    string Intent);
