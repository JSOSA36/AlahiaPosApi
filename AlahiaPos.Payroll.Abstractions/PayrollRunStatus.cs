namespace AlahiaPos.Payroll.Abstractions;

/// <summary>
/// Ciclo de vida del <see cref="PayrollRun"/> (ALAHIA-PE-01 §9).
/// </summary>
public enum PayrollRunStatus
{
    Draft = 0,
    Approved = 1
}
