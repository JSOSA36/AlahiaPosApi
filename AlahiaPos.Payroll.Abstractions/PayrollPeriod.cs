namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Período de liquidación (clave de negocio, no tabla).
/// </summary>
public sealed record PayrollPeriod(
    DateOnly StartInclusive,
    DateOnly EndInclusive,
    string PeriodKey);
